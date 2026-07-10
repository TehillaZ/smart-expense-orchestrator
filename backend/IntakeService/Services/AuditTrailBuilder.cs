using IntakeService.Models;

namespace IntakeService.Services;

public static class AuditTrailBuilder
{
    public static AuditTrailResponse Build(
        SubmissionStatusResponse submission,
        NotificationLookupResponse? notification)
    {
        var rules = submission.CitedRules?.ToArray() ?? [];
        var humanDecision = BuildHumanDecision(submission);
        var payment = BuildPaymentSnapshot(submission, notification);
        var timeline = BuildTimeline(submission, humanDecision, payment);

        return new AuditTrailResponse(
            submission.TrackingId,
            submission.CorrelationId,
            new AuditInvoiceSnapshot(
                submission.TrackingId,
                submission.Vendor,
                submission.InvoiceNumber,
                submission.Amount,
                submission.Category),
            submission.PolicyVersion ?? "v1",
            submission.AutonomyCeilingAtDecision,
            rules,
            new AuditAiSnapshot(
                submission.AiRecommendation,
                submission.AiConfidence,
                rules,
                BuildAiReasoningSummary(submission)),
            humanDecision,
            payment,
            timeline);
    }

    private static string BuildAiReasoningSummary(SubmissionStatusResponse submission)
    {
        if (IsPaymentTerminalStatus(submission.Status))
        {
            return submission.AiRecommendation is null
                ? "AI recommendation was recorded before payment processing."
                : $"AI recommended {submission.AiRecommendation} before payment processing.";
        }

        return submission.Reason;
    }

    private static bool IsPaymentTerminalStatus(string status)
    {
        return status is WorkflowStatuses.Paid
            or WorkflowStatuses.PaymentFailed
            or WorkflowStatuses.Compensated
            or WorkflowStatuses.PaymentPending;
    }

    private static AuditHumanDecisionSnapshot? BuildHumanDecision(SubmissionStatusResponse submission)
    {
        if (!string.IsNullOrWhiteSpace(submission.HumanDecision))
        {
            return new AuditHumanDecisionSnapshot(
                submission.HumanDecision,
                submission.HumanDecisionComment,
                "Human Approver");
        }

        if (string.Equals(submission.Status, WorkflowStatuses.HumanApproved, StringComparison.Ordinal))
        {
            return new AuditHumanDecisionSnapshot(
                "Approve",
                ExtractComment(submission.Reason),
                "Human Approver");
        }

        if (string.Equals(submission.Status, WorkflowStatuses.Rejected, StringComparison.Ordinal))
        {
            return new AuditHumanDecisionSnapshot(
                "Reject",
                ExtractComment(submission.Reason),
                "Human Approver");
        }

        if (submission.Reason.Contains("human reviewer", StringComparison.OrdinalIgnoreCase)
            || submission.Reason.Contains("More information requested", StringComparison.OrdinalIgnoreCase))
        {
            return new AuditHumanDecisionSnapshot(
                submission.Reason.Contains("rejected", StringComparison.OrdinalIgnoreCase)
                    ? "Reject"
                    : submission.Reason.Contains("More information requested", StringComparison.OrdinalIgnoreCase)
                        ? "RequestInfo"
                        : "Approve",
                ExtractComment(submission.Reason),
                "Human Approver");
        }

        if (IsPaidAfterHumanEscalation(submission))
        {
            return new AuditHumanDecisionSnapshot(
                "Approve",
                submission.HumanDecisionComment,
                "Human Approver");
        }

        return null;
    }

    private static bool IsPaidAfterHumanEscalation(SubmissionStatusResponse submission)
    {
        var paymentAfterEscalation = submission.Status is WorkflowStatuses.Paid
            or WorkflowStatuses.PaymentPending
            or WorkflowStatuses.Compensated
            or WorkflowStatuses.PaymentFailed;

        return paymentAfterEscalation
            && string.Equals(submission.AiRecommendation, "Escalate", StringComparison.OrdinalIgnoreCase);
    }

    private static AuditPaymentSnapshot? BuildPaymentSnapshot(
        SubmissionStatusResponse submission,
        NotificationLookupResponse? notification)
    {
        if (notification is not null)
        {
            return new AuditPaymentSnapshot(
                notification.Status,
                notification.Reason,
                notification.OutcomeType,
                notification.Channel,
                notification.SentAt);
        }

        if (submission.Status is WorkflowStatuses.Paid
            or WorkflowStatuses.PaymentFailed
            or WorkflowStatuses.Compensated
            or WorkflowStatuses.PaymentPending)
        {
            return new AuditPaymentSnapshot(
                submission.Status,
                submission.Reason,
                null,
                null,
                null);
        }

        return null;
    }

    private static IReadOnlyList<AuditTimelineEvent> BuildTimeline(
        SubmissionStatusResponse submission,
        AuditHumanDecisionSnapshot? humanDecision,
        AuditPaymentSnapshot? payment)
    {
        var events = new List<AuditTimelineEvent>
        {
            new(
                "Received",
                "Invoice received and queued for policy review.",
                "Intake Service"),
        };

        if (!string.Equals(submission.Status, WorkflowStatuses.Received, StringComparison.Ordinal))
        {
            events.Add(new AuditTimelineEvent(
                "Processing",
                "Submission entered policy and AI evaluation.",
                "Approval Service"));
        }

        if (submission.AiRecommendation is not null)
        {
            events.Add(new AuditTimelineEvent(
                "AiRecommendation",
                $"AI recommended {submission.AiRecommendation} with confidence {(submission.AiConfidence ?? 0m):0.00}.",
                "Stub AI Provider",
                IsPaymentTerminalStatus(submission.Status) ? null : submission.Reason));
        }

        if (submission.Status is WorkflowStatuses.AutoApproved or WorkflowStatuses.Paid
            && humanDecision is null
            && !WasEscalatedForHumanReview(submission))
        {
            events.Add(new AuditTimelineEvent(
                "PolicyDecision",
                "Deterministic policy engine auto-approved the expense.",
                "Policy Engine",
                FormatRules(submission.CitedRules)));
        }

        if (WasEscalatedForHumanReview(submission)
            && submission.Status is not WorkflowStatuses.Received and not WorkflowStatuses.Processing)
        {
            events.Add(new AuditTimelineEvent(
                "PolicyDecision",
                submission.Status is WorkflowStatuses.Escalated
                    ? submission.Reason
                    : "Submission was escalated for human review based on policy and AI evaluation.",
                "Policy Engine",
                FormatRules(submission.CitedRules)));
        }

        if (humanDecision is not null)
        {
            events.Add(new AuditTimelineEvent(
                "HumanDecision",
                FormatHumanDecisionSummary(humanDecision),
                humanDecision.Actor,
                humanDecision.Comment));
        }

        if (payment is not null)
        {
            AppendPaymentTimelineEvents(events, payment);
        }

        return events;
    }

    private static void AppendPaymentTimelineEvents(
        List<AuditTimelineEvent> events,
        AuditPaymentSnapshot payment)
    {
        if (string.Equals(payment.Status, WorkflowStatuses.Compensated, StringComparison.Ordinal))
        {
            events.Add(new AuditTimelineEvent(
                "PaymentFailed",
                "Payment charge failed after approval.",
                "Payment Service",
                payment.Reason));
            events.Add(new AuditTimelineEvent(
                "Compensated",
                "OK — you get your money back. Reserved funds were released and this expense was not paid.",
                "Payment Service",
                payment.OutcomeType));
            return;
        }

        if (string.Equals(payment.Status, WorkflowStatuses.PaymentFailed, StringComparison.Ordinal))
        {
            events.Add(new AuditTimelineEvent(
                "PaymentFailed",
                "Payment charge failed.",
                "Payment Service",
                payment.Reason));
            return;
        }

        events.Add(new AuditTimelineEvent(
            "Payment",
            payment.Reason,
            "Payment Service",
            payment.OutcomeType));
    }

    private static bool WasEscalatedForHumanReview(SubmissionStatusResponse submission)
    {
        return string.Equals(submission.Status, WorkflowStatuses.Escalated, StringComparison.Ordinal)
            || string.Equals(submission.AiRecommendation, "Escalate", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrWhiteSpace(submission.HumanDecision);
    }

    private static string FormatHumanDecisionSummary(AuditHumanDecisionSnapshot humanDecision)
    {
        return humanDecision.Decision switch
        {
            "Approve" => "Expense approved by a human reviewer.",
            "Reject" => "Expense rejected by a human reviewer.",
            "RequestInfo" => "More information requested before a final decision.",
            _ => $"Human reviewer decision: {humanDecision.Decision}.",
        };
    }

    private static string? ExtractComment(string reason)
    {
        const string notePrefix = "Note: ";
        var noteIndex = reason.IndexOf(notePrefix, StringComparison.OrdinalIgnoreCase);
        if (noteIndex >= 0)
        {
            return reason[(noteIndex + notePrefix.Length)..].Trim();
        }

        const string requestedPrefix = "More information requested: ";
        var requestedIndex = reason.IndexOf(requestedPrefix, StringComparison.OrdinalIgnoreCase);
        if (requestedIndex >= 0)
        {
            return reason[(requestedIndex + requestedPrefix.Length)..].Trim();
        }

        return null;
    }

    private static string? FormatRules(IReadOnlyList<string>? rules)
    {
        if (rules is null || rules.Count == 0)
        {
            return null;
        }

        return string.Join(", ", rules);
    }
}
