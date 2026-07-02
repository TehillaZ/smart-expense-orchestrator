using ApprovalService.Models;
using ApprovalService.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApprovalService.Tests;

public class SubmissionReceivedHandlerTests
{
    private static SubmissionReceivedEvent SampleEvent(string trackingId = "tracking-1") =>
        new(trackingId, "correlation-1", "Acme Corp", "INV-1001", 100m, "Office Supplies");

    [Fact]
    public async Task HandleAsync_WithNoExistingState_CreatesProcessingState()
    {
        var store = new FakeSubmissionStateStore();
        var handler = new SubmissionReceivedHandler(store, NullLogger<SubmissionReceivedHandler>.Instance);

        await handler.HandleAsync(SampleEvent());

        Assert.NotNull(store.LastSaved);
        Assert.Equal("Processing", store.LastSaved!.Status);
        Assert.Equal("tracking-1", store.LastSaved.TrackingId);
        Assert.Equal("Acme Corp", store.LastSaved.Vendor);
    }

    [Fact]
    public async Task HandleAsync_WithExistingState_PreservesOriginalFieldsAndUpdatesStatus()
    {
        var store = new FakeSubmissionStateStore();
        store.Seed(new SubmissionState(
            "tracking-2",
            "correlation-2",
            "Acme Corp",
            "INV-2002",
            500m,
            "Travel",
            "Received",
            "Invoice received and is pending analysis."));
        var handler = new SubmissionReceivedHandler(store, NullLogger<SubmissionReceivedHandler>.Instance);

        await handler.HandleAsync(SampleEvent("tracking-2"));

        Assert.NotNull(store.LastSaved);
        Assert.Equal("Processing", store.LastSaved!.Status);
        Assert.Equal(500m, store.LastSaved.Amount);
        Assert.Equal("Travel", store.LastSaved.Category);
    }
}
