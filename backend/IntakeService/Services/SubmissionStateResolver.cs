using IntakeService.Models;

namespace IntakeService.Services;

public static class SubmissionStateResolver
{
    private static readonly IReadOnlyDictionary<string, int> StatusRank =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [WorkflowStatuses.Received] = 0,
            [WorkflowStatuses.Processing] = 1,
            [WorkflowStatuses.Escalated] = 2,
            [WorkflowStatuses.AutoApproved] = 3,
            [WorkflowStatuses.HumanApproved] = 4,
            [WorkflowStatuses.Rejected] = 5,
            [WorkflowStatuses.PaymentPending] = 6,
            [WorkflowStatuses.Paid] = 7,
            [WorkflowStatuses.PaymentFailed] = 8,
            [WorkflowStatuses.Compensated] = 9,
        };

    public static SubmissionStatusResponse? Resolve(
        SubmissionStatusResponse? localState,
        SubmissionStatusResponse? workflowState)
    {
        if (localState is null)
        {
            return workflowState;
        }

        if (workflowState is null)
        {
            return localState;
        }

        var localRank = GetRank(localState);
        var workflowRank = GetRank(workflowState);

        SubmissionStatusResponse resolved;

        if (workflowRank > localRank)
        {
            resolved = workflowState;
        }
        else if (localRank > workflowRank)
        {
            resolved = localState;
        }
        else
        {
            resolved = HasRicherContext(workflowState, localState) ? workflowState : localState;
        }

        return PreserveIntakeMetadata(localState, resolved);
    }

    private static SubmissionStatusResponse PreserveIntakeMetadata(
        SubmissionStatusResponse localState,
        SubmissionStatusResponse resolved)
    {
        if (localState.DuplicateResubmitCount <= resolved.DuplicateResubmitCount)
        {
            return resolved;
        }

        return resolved with { DuplicateResubmitCount = localState.DuplicateResubmitCount };
    }

    public static (string Status, string DecisionType) ToRecentActivityFields(
        SubmissionStatusResponse submission)
    {
        if (!string.IsNullOrWhiteSpace(submission.HumanDecision))
        {
            return submission.HumanDecision switch
            {
                "Approve" => (submission.Status, "HumanApproved"),
                "Reject" => (submission.Status, "Rejected"),
                "RequestInfo" => (submission.Status, "Escalated"),
                _ => (submission.Status, submission.HumanDecision),
            };
        }

        if (string.Equals(submission.Status, WorkflowStatuses.Paid, StringComparison.Ordinal)
            && string.Equals(submission.AiRecommendation, "Escalate", StringComparison.OrdinalIgnoreCase))
        {
            return (submission.Status, "HumanApproved");
        }

        return submission.Status switch
        {
            WorkflowStatuses.AutoApproved => (submission.Status, "AutoApproved"),
            WorkflowStatuses.HumanApproved => (submission.Status, "HumanApproved"),
            WorkflowStatuses.Escalated => (submission.Status, "Escalated"),
            WorkflowStatuses.Rejected => (submission.Status, "Rejected"),
            WorkflowStatuses.PaymentFailed or WorkflowStatuses.Compensated =>
                (submission.Status, "PaymentFailed"),
            _ => (submission.Status, "Received"),
        };
    }

    private static int GetRank(SubmissionStatusResponse submission)
    {
        return StatusRank.TryGetValue(submission.Status, out var rank) ? rank : -1;
    }

    private static bool HasRicherContext(
        SubmissionStatusResponse candidate,
        SubmissionStatusResponse current)
    {
        if (candidate.AiRecommendation is not null && current.AiRecommendation is null)
        {
            return true;
        }

        if (candidate.HumanDecision is not null && current.HumanDecision is null)
        {
            return true;
        }

        if (candidate.HumanDecisionActor is not null && current.HumanDecisionActor is null)
        {
            return true;
        }

        if (candidate.CitedRules is { Count: > 0 } && current.CitedRules is not { Count: > 0 })
        {
            return true;
        }

        if (candidate.AutonomyCeilingAtDecision is not null && current.AutonomyCeilingAtDecision is null)
        {
            return true;
        }

        return false;
    }
}
