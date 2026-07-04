using NotificationService.Models;

namespace NotificationService.Services;

public sealed class StubNotificationChannel : INotificationChannel
{
    private readonly ILogger<StubNotificationChannel> logger;

    public StubNotificationChannel(ILogger<StubNotificationChannel> logger)
    {
        this.logger = logger;
    }

    public Task SendAsync(NotificationRecord record, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Stub notification sent for submission {TrackingId} with status {Status}, outcome {OutcomeType}, and reason {Reason}.",
            record.TrackingId,
            record.Status,
            record.OutcomeType,
            record.Reason);

        return Task.CompletedTask;
    }
}
