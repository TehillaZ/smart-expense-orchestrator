using Dapr.Client;
using NotificationService.Models;

namespace NotificationService.Services;

public sealed class DaprNotificationStore : INotificationStore
{
    private const string StateStoreName = "approval-state";
    private const string KeyPrefix = "notification";

    private readonly DaprClient daprClient;
    private readonly ILogger<DaprNotificationStore> logger;

    public DaprNotificationStore(DaprClient daprClient, ILogger<DaprNotificationStore> logger)
    {
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public async Task<NotificationRecord?> GetAsync(string trackingId, string outcomeType)
    {
        try
        {
            return await daprClient.GetStateAsync<NotificationRecord>(
                StateStoreName,
                BuildKey(trackingId, outcomeType));
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to read notification for submission {TrackingId} and outcome {OutcomeType}.",
                trackingId,
                outcomeType);
            throw;
        }
    }

    public async Task SaveAsync(NotificationRecord record)
    {
        try
        {
            await daprClient.SaveStateAsync(
                StateStoreName,
                BuildKey(record.TrackingId, record.OutcomeType),
                record);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to save notification for submission {TrackingId} and outcome {OutcomeType}.",
                record.TrackingId,
                record.OutcomeType);
            throw;
        }
    }

    private static string BuildKey(string trackingId, string outcomeType) =>
        $"{KeyPrefix}:{trackingId}:{outcomeType}";
}
