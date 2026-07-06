using IntakeService.Models;
using IntakeService.Services;

namespace IntakeService.Tests;

public class AuditTrailBuilderTests
{
    [Fact]
    public void Build_IncludesAutoApprovalPolicyDecisionAndAiDetails()
    {
        var submission = new SubmissionStatusResponse(
            "tracking-1",
            "correlation-1",
            "Acme Corp",
            "INV-1001",
            100m,
            "Meals",
            WorkflowStatuses.Paid,
            "Payment of $100.00 completed successfully.",
            "Approve",
            0.95m,
            ["MEAL-01"],
            "v1");

        var audit = AuditTrailBuilder.Build(submission, notification: null);

        Assert.Equal("correlation-1", audit.CorrelationId);
        Assert.Equal("Approve", audit.Ai.Recommendation);
        Assert.Contains(audit.Timeline, item => item.Step == "PolicyDecision");
        Assert.Contains(audit.Timeline, item => item.Step == "AiRecommendation");
        Assert.Null(audit.HumanDecision);
    }

    [Fact]
    public void Build_IncludesHumanDecisionForRejectedSubmission()
    {
        var submission = new SubmissionStatusResponse(
            "tracking-2",
            "correlation-2",
            "Beta LLC",
            "INV-2002",
            500m,
            "Travel",
            WorkflowStatuses.Rejected,
            "Expense rejected by a human reviewer. Note: Missing receipt.",
            "Escalate",
            0.88m,
            ["AUTONOMY-CEILING"],
            "v1");

        var audit = AuditTrailBuilder.Build(submission, notification: null);

        Assert.NotNull(audit.HumanDecision);
        Assert.Equal("Reject", audit.HumanDecision!.Decision);
        Assert.Contains(audit.Timeline, item => item.Step == "HumanDecision");
    }

    [Fact]
    public void Build_IncludesHumanDecisionForPaidSubmissionAfterEscalation()
    {
        var submission = new SubmissionStatusResponse(
            "tracking-4",
            "correlation-4",
            "Delta Co",
            "INV-4004",
            9500m,
            "Travel",
            WorkflowStatuses.Paid,
            "Payment of $9,500.00 completed successfully.",
            "Escalate",
            0.82m,
            ["TRAVEL-02"],
            "v1",
            HumanDecision: "Approve",
            HumanDecisionComment: "Approved after receipt review.");

        var audit = AuditTrailBuilder.Build(submission, notification: null);

        Assert.NotNull(audit.HumanDecision);
        Assert.Equal("Approve", audit.HumanDecision!.Decision);
        Assert.Equal("Approved after receipt review.", audit.HumanDecision.Comment);
        Assert.Contains(audit.Timeline, item => item.Step == "HumanDecision");
        Assert.Contains(audit.Timeline, item => item.Step == "PolicyDecision");
    }

    [Fact]
    public void Build_IncludesPaymentSnapshotFromNotification()
    {
        var submission = new SubmissionStatusResponse(
            "tracking-3",
            "correlation-3",
            "Gamma Inc",
            "INV-3003",
            9500m,
            "Travel",
            WorkflowStatuses.Compensated,
            "Payment failed and reservation was compensated.",
            "Escalate",
            0.80m,
            ["TRAVEL-02"],
            "v1");

        var notification = new NotificationLookupResponse(
            "tracking-3",
            "correlation-3",
            WorkflowStatuses.Compensated,
            "Payment failed and reservation was compensated.",
            9500m,
            "payment.failed",
            "stub",
            DateTimeOffset.UtcNow);

        var audit = AuditTrailBuilder.Build(submission, notification);

        Assert.NotNull(audit.Payment);
        Assert.Equal("payment.failed", audit.Payment!.OutcomeType);
        Assert.Contains(audit.Timeline, item => item.Step == "Payment");
    }
}
