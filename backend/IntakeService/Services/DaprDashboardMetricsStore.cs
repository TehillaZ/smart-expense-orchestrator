using Dapr.Client;
using IntakeService.Models;

namespace IntakeService.Services;

public sealed class DaprDashboardMetricsStore : IDashboardMetricsStore
{
    private const string StateStoreName = "approval-state";
    private const string MetricsKey = "dashboard:metrics";
    private const string RecentActivityKey = "dashboard:recent-activity";
    private const string ProcessedEventsKeyPrefix = "dashboard:processed:";

    private readonly DaprClient daprClient;
    private readonly ILogger<DaprDashboardMetricsStore> logger;

    public DaprDashboardMetricsStore(DaprClient daprClient, ILogger<DaprDashboardMetricsStore> logger)
    {
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public async Task<DashboardReadModel> GetReadModelAsync()
    {
        var metrics = await daprClient.GetStateAsync<DashboardMetrics>(StateStoreName, MetricsKey)
            ?? DashboardMetrics.Empty;
        var recentActivity = await daprClient.GetStateAsync<List<RecentActivityItem>>(StateStoreName, RecentActivityKey)
            ?? [];

        return new DashboardReadModel(metrics, recentActivity);
    }

    public async Task ApplyEventAsync(MetricsRecordRequest request)
    {
        var processedKey = BuildProcessedKey(request.TrackingId, request.EventType);
        var alreadyProcessed = await daprClient.GetStateAsync<bool>(StateStoreName, processedKey);
        if (alreadyProcessed)
        {
            logger.LogInformation(
                "Skipping duplicate dashboard metrics event {EventType} for submission {TrackingId}.",
                request.EventType,
                request.TrackingId);
            return;
        }

        var readModel = await GetReadModelAsync();
        var updatedMetrics = DashboardMetricsProjector.ApplyEvent(readModel.Metrics, request);
        var updatedRecentActivity = DashboardMetricsProjector.PrependRecentActivity(
            readModel.RecentActivity,
            request);

        await daprClient.SaveStateAsync(StateStoreName, MetricsKey, updatedMetrics);
        await daprClient.SaveStateAsync(StateStoreName, RecentActivityKey, updatedRecentActivity);
        await daprClient.SaveStateAsync(StateStoreName, processedKey, true);

        logger.LogInformation(
            "Applied dashboard metrics event {EventType} for submission {TrackingId} with correlation {CorrelationId}.",
            request.EventType,
            request.TrackingId,
            request.CorrelationId);
    }

    private static string BuildProcessedKey(string trackingId, string eventType) =>
        $"{ProcessedEventsKeyPrefix}{DashboardMetricsProjector.BuildProcessedEventKey(trackingId, eventType)}";
}
