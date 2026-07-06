using ApprovalService.Models;
using ApprovalService.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApprovalService.Tests;

public class ApprovalDecisionHandlerTests
{
    [Fact]
    public async Task ApplyDecisionAsync_WithApprove_UpdatesStatusAndRemovesFromQueue()
    {
        var store = new FakeSubmissionStateStore();
        store.Seed(CreateEscalatedState());
        await store.AddToEscalationQueueAsync("tracking-1");
        var publisher = new FakePaymentRequestPublisher();
        var handler = new ApprovalDecisionHandler(
            store,
            publisher,
            new NoOpMetricsRecorder(),
            NullLogger<ApprovalDecisionHandler>.Instance);

        var (response, errors) = await handler.ApplyDecisionAsync(
            "tracking-1",
            new ApprovalDecisionRequest("Approve", "Looks good", "correlation-1"));

        Assert.Empty(errors);
        Assert.NotNull(response);
        Assert.Equal(ApprovalStatuses.HumanApproved, response!.Status);
        Assert.Empty(await store.GetEscalatedSubmissionsAsync());
        Assert.Single(publisher.PublishedStates);
    }

    [Fact]
    public async Task ApplyDecisionAsync_WithMissingSubmission_ReturnsNotFoundError()
    {
        var store = new FakeSubmissionStateStore();
        var handler = new ApprovalDecisionHandler(
            store,
            new FakePaymentRequestPublisher(),
            new NoOpMetricsRecorder(),
            NullLogger<ApprovalDecisionHandler>.Instance);

        var (response, errors) = await handler.ApplyDecisionAsync(
            "missing-id",
            new ApprovalDecisionRequest("Approve", null, null));

        Assert.Null(response);
        Assert.Contains("submission was not found.", errors);
    }

    private static SubmissionState CreateEscalatedState() =>
        new(
            "tracking-1",
            "correlation-1",
            "Acme Corp",
            "INV-1003",
            500m,
            "meals",
            ApprovalStatuses.Escalated,
            "Escalated for human review.",
            "Escalate",
            0.88m,
            ["AUTONOMY-CEILING"],
            "v1");
}
