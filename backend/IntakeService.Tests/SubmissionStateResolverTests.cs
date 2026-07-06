using IntakeService.Models;
using IntakeService.Services;

namespace IntakeService.Tests;

public class SubmissionStateResolverTests
{
    [Fact]
    public void Resolve_PrefersWorkflowStateWhenLocalStateIsStillReceived()
    {
        var local = new SubmissionStatusResponse(
            "tracking-1",
            "correlation-1",
            "Acme Corp",
            "INV-1001",
            100m,
            "Meals",
            WorkflowStatuses.Received,
            "Invoice received and is pending analysis.");

        var workflow = new SubmissionStatusResponse(
            "tracking-1",
            "correlation-1",
            "Acme Corp",
            "INV-1001",
            100m,
            "Meals",
            WorkflowStatuses.Paid,
            "Payment of $100.00 completed successfully.",
            "Escalate",
            0.91m,
            ["AUTONOMY-CEILING"],
            "v1",
            HumanDecision: "Approve",
            HumanDecisionComment: "Approved.");

        var resolved = SubmissionStateResolver.Resolve(local, workflow);

        Assert.NotNull(resolved);
        Assert.Equal(WorkflowStatuses.Paid, resolved!.Status);
        Assert.Equal("Approve", resolved.HumanDecision);
    }

    [Fact]
    public void ToRecentActivityFields_UsesHumanDecisionForPaidSubmission()
    {
        var submission = new SubmissionStatusResponse(
            "tracking-2",
            "correlation-2",
            "Beta LLC",
            "INV-2002",
            450m,
            "Travel",
            WorkflowStatuses.Paid,
            "Payment of $450.00 completed successfully.",
            "Escalate",
            0.75m,
            HumanDecision: "Approve");

        var (status, decisionType) = SubmissionStateResolver.ToRecentActivityFields(submission);

        Assert.Equal(WorkflowStatuses.Paid, status);
        Assert.Equal("HumanApproved", decisionType);
    }
}
