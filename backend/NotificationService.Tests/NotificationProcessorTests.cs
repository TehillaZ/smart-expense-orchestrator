using Microsoft.Extensions.Logging.Abstractions;
using NotificationService.Models;
using NotificationService.Services;

namespace NotificationService.Tests;

public class NotificationProcessorTests
{
    private static PaymentCompletedEvent SampleCompletedEvent() =>
        new("tracking-1", "correlation-1", "Paid", "Payment completed successfully.", 100m);

    private static PaymentFailedEvent SampleFailedEvent() =>
        new("tracking-2", "correlation-2", "Compensated", "Payment failed and was compensated.", 9500m);

    [Fact]
    public async Task ProcessPaymentCompletedAsync_StoresAndSendsNotification()
    {
        var store = new InMemoryNotificationStore();
        var channel = new FakeNotificationChannel();
        var processor = CreateProcessor(store, channel);

        await processor.ProcessPaymentCompletedAsync(SampleCompletedEvent());

        var saved = await store.GetAsync("tracking-1", NotificationOutcomeTypes.PaymentCompleted);
        Assert.NotNull(saved);
        Assert.Equal("Paid", saved!.Status);
        Assert.Equal(NotificationChannels.Stub, saved.Channel);
        Assert.Single(channel.SentRecords);
    }

    [Fact]
    public async Task ProcessPaymentFailedAsync_StoresAndSendsNotification()
    {
        var store = new InMemoryNotificationStore();
        var channel = new FakeNotificationChannel();
        var processor = CreateProcessor(store, channel);

        await processor.ProcessPaymentFailedAsync(SampleFailedEvent());

        var saved = await store.GetAsync("tracking-2", NotificationOutcomeTypes.PaymentFailed);
        Assert.NotNull(saved);
        Assert.Equal("Compensated", saved!.Status);
        Assert.Single(channel.SentRecords);
    }

    [Fact]
    public async Task ProcessPaymentCompletedAsync_WithDuplicateEvent_DoesNotSendAgain()
    {
        var store = new InMemoryNotificationStore();
        var channel = new FakeNotificationChannel();
        var processor = CreateProcessor(store, channel);

        await processor.ProcessPaymentCompletedAsync(SampleCompletedEvent());
        await processor.ProcessPaymentCompletedAsync(SampleCompletedEvent());

        Assert.Single(channel.SentRecords);
    }

    private static NotificationProcessor CreateProcessor(
        INotificationStore store,
        INotificationChannel channel) =>
        new(
            store,
            channel,
            NullLogger<NotificationProcessor>.Instance);
}

internal sealed class FakeNotificationChannel : INotificationChannel
{
    public List<NotificationRecord> SentRecords { get; } = [];

    public Task SendAsync(NotificationRecord record, CancellationToken cancellationToken = default)
    {
        SentRecords.Add(record);
        return Task.CompletedTask;
    }
}
