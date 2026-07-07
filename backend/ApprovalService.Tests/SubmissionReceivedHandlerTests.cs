using ApprovalService.Models;
using ApprovalService.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApprovalService.Tests;

public class SubmissionReceivedHandlerTests
{
    private static SubmissionReceivedEvent SampleEvent(string trackingId = "tracking-1", decimal amount = 100m, string category = "Office Supplies") =>
        new(trackingId, "correlation-1", "Acme Corp", "INV-1001", amount, category);

    [Fact]
    public async Task HandleAsync_WithEligibleSubmission_AutoApproves()
    {
        var store = new FakeSubmissionStateStore();
        var settings = new FakePolicySettingsStore();
        var publisher = new FakePaymentRequestPublisher();
        var processor = CreateProcessor(store, settings, publisher);

        await processor.ProcessAsync(SampleEvent());

        Assert.NotNull(store.LastSaved);
        Assert.Equal(ApprovalStatuses.AutoApproved, store.LastSaved!.Status);
        Assert.Equal("Approve", store.LastSaved.AiRecommendation);
        Assert.Single(publisher.PublishedStates);
    }

    [Fact]
    public async Task HandleAsync_WithAmountAboveCeiling_Escalates()
    {
        var store = new FakeSubmissionStateStore();
        var settings = new FakePolicySettingsStore();
        var processor = CreateProcessor(store, settings, new FakePaymentRequestPublisher());

        await processor.ProcessAsync(SampleEvent(amount: 500m, category: "meals"));

        Assert.NotNull(store.LastSaved);
        Assert.Equal(ApprovalStatuses.Escalated, store.LastSaved!.Status);
    }

    [Fact]
    public async Task HandleAsync_WithCapitalHardware_EscalatesEvenWhenAiRecommendsApprove()
    {
        var store = new FakeSubmissionStateStore();
        var settings = new FakePolicySettingsStore();
        var processor = CreateProcessor(store, settings, new FakePaymentRequestPublisher());

        await processor.ProcessAsync(SampleEvent(amount: 1400m, category: "hardware"));

        Assert.NotNull(store.LastSaved);
        Assert.Equal(ApprovalStatuses.Escalated, store.LastSaved!.Status);
        Assert.Contains("HW-02", store.LastSaved.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_WhenAiProviderThrows_EscalatesForHumanReview()
    {
        var store = new FakeSubmissionStateStore();
        var settings = new FakePolicySettingsStore();
        var processor = CreateProcessor(
            store,
            settings,
            new FakePaymentRequestPublisher(),
            new ThrowingInvoiceAiProvider());

        await processor.ProcessAsync(SampleEvent());

        Assert.NotNull(store.LastSaved);
        Assert.Equal(ApprovalStatuses.Escalated, store.LastSaved!.Status);
        Assert.Contains("Policy evaluation failed", store.LastSaved.Reason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Escalate", store.LastSaved.AiRecommendation);
        Assert.NotNull(store.LastSaved.CitedRules);
        Assert.Contains("POLICY-ERROR", store.LastSaved.CitedRules);
    }

    [Fact]
    public async Task HandleAsync_WithAlreadyAutoApprovedSubmission_SkipsReprocessingAndRepublish()
    {
        var store = new FakeSubmissionStateStore();
        store.Seed(new SubmissionState(
            "tracking-1",
            "correlation-1",
            "Acme Corp",
            "INV-1001",
            100m,
            "Office Supplies",
            ApprovalStatuses.AutoApproved,
            "Auto approved.",
            "Approve",
            0.95m,
            ["MEAL-01"],
            "v1"));
        var settings = new FakePolicySettingsStore();
        var publisher = new FakePaymentRequestPublisher();
        var processor = CreateProcessor(store, settings, publisher);

        await processor.ProcessAsync(SampleEvent());

        var current = await store.GetAsync("tracking-1");
        Assert.NotNull(current);
        Assert.Equal(ApprovalStatuses.AutoApproved, current!.Status);
        Assert.Null(store.LastSaved);
        Assert.Empty(publisher.PublishedStates);
    }

    private static ApprovalWorkflowProcessor CreateProcessor(
        FakeSubmissionStateStore store,
        FakePolicySettingsStore settings,
        FakePaymentRequestPublisher publisher,
        IInvoiceAiProvider? aiProvider = null)
    {
        return new ApprovalWorkflowProcessor(
            store,
            settings,
            aiProvider ?? new StubInvoiceAiProvider(),
            new PolicyEngine(),
            publisher,
            new NoOpMetricsRecorder(),
            NullLogger<ApprovalWorkflowProcessor>.Instance);
    }

    private sealed class ThrowingInvoiceAiProvider : IInvoiceAiProvider
    {
        public Task<AiRecommendationResult> GetRecommendationAsync(
            SubmissionReceivedEvent submission,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Simulated LLM provider outage.");
        }
    }
}
