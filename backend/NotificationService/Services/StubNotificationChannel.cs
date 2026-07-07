using NotificationService.Models;

namespace NotificationService.Services;

public sealed class StubNotificationChannel : INotificationChannel
{
    private readonly NotificationChannelOptions channelOptions;
    private readonly ILogger<StubNotificationChannel> logger;

    public StubNotificationChannel(
        NotificationChannelOptions channelOptions,
        ILogger<StubNotificationChannel> logger)
    {
        this.channelOptions = channelOptions;
        this.logger = logger;
    }

    public Task SendAsync(NotificationRecord record, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Stub notification sent for submission {TrackingId} with status {Status}, outcome {OutcomeType}, reason {Reason}, sender {SenderAddress}.",
            record.TrackingId,
            record.Status,
            record.OutcomeType,
            record.Reason,
            channelOptions.SenderAddress ?? "not-configured");

        return Task.CompletedTask;
    }
}
