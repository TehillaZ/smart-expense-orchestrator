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
        var processor = CreateProcessor(store, settings);

        await processor.ProcessAsync(SampleEvent());

        Assert.NotNull(store.LastSaved);
        Assert.Equal(ApprovalStatuses.AutoApproved, store.LastSaved!.Status);
        Assert.Equal("Approve", store.LastSaved.AiRecommendation);
    }

    [Fact]
    public async Task HandleAsync_WithAmountAboveCeiling_Escalates()
    {
        var store = new FakeSubmissionStateStore();
        var settings = new FakePolicySettingsStore();
        var processor = CreateProcessor(store, settings);

        await processor.ProcessAsync(SampleEvent(amount: 500m, category: "meals"));

        Assert.NotNull(store.LastSaved);
        Assert.Equal(ApprovalStatuses.Escalated, store.LastSaved!.Status);
    }

    [Fact]
    public async Task HandleAsync_WithCapitalHardware_EscalatesEvenWhenAiRecommendsApprove()
    {
        var store = new FakeSubmissionStateStore();
        var settings = new FakePolicySettingsStore();
        var processor = CreateProcessor(store, settings);

        await processor.ProcessAsync(SampleEvent(amount: 1400m, category: "hardware"));

        Assert.NotNull(store.LastSaved);
        Assert.Equal(ApprovalStatuses.Escalated, store.LastSaved!.Status);
        Assert.Contains("HW-02", store.LastSaved.Reason, StringComparison.OrdinalIgnoreCase);
    }

    private static ApprovalWorkflowProcessor CreateProcessor(
        FakeSubmissionStateStore store,
        FakePolicySettingsStore settings)
    {
        return new ApprovalWorkflowProcessor(
            store,
            settings,
            new StubInvoiceAiProvider(),
            new PolicyEngine(),
            NullLogger<ApprovalWorkflowProcessor>.Instance);
    }
}
