using NotificationService.Models;

namespace NotificationService.Services;

public sealed class NotificationProcessor
{
    private readonly INotificationStore notificationStore;
    private readonly INotificationChannel notificationChannel;
    private readonly ILogger<NotificationProcessor> logger;

    public NotificationProcessor(
        INotificationStore notificationStore,
        INotificationChannel notificationChannel,
        ILogger<NotificationProcessor> logger)
    {
        this.notificationStore = notificationStore;
        this.notificationChannel = notificationChannel;
        this.logger = logger;
    }

    public Task ProcessPaymentCompletedAsync(PaymentCompletedEvent paymentEvent) =>
        ProcessOutcomeAsync(
            paymentEvent.TrackingId,
            paymentEvent.CorrelationId,
            paymentEvent.Status,
            paymentEvent.Reason,
            paymentEvent.Amount,
            NotificationOutcomeTypes.PaymentCompleted);

    public Task ProcessPaymentFailedAsync(PaymentFailedEvent paymentEvent) =>
        ProcessOutcomeAsync(
            paymentEvent.TrackingId,
            paymentEvent.CorrelationId,
            paymentEvent.Status,
            paymentEvent.Reason,
            paymentEvent.Amount,
            NotificationOutcomeTypes.PaymentFailed);

    private async Task ProcessOutcomeAsync(
        string trackingId,
        string correlationId,
        string status,
        string reason,
        decimal amount,
        string outcomeType)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["TrackingId"] = trackingId,
            ["OutcomeType"] = outcomeType
        });

        var existing = await notificationStore.GetAsync(trackingId, outcomeType);
        if (existing is not null)
        {
            logger.LogInformation(
                "Skipping duplicate notification for submission {TrackingId} and outcome {OutcomeType}.",
                trackingId,
                outcomeType);
            return;
        }

        var record = new NotificationRecord(
            trackingId,
            correlationId,
            status,
            reason,
            amount,
            outcomeType,
            NotificationChannels.Stub,
            DateTimeOffset.UtcNow);

        await notificationChannel.SendAsync(record);
        await notificationStore.SaveAsync(record);

        logger.LogInformation(
            "Recorded notification for submission {TrackingId} with status {Status}.",
            trackingId,
            status);
    }
}
