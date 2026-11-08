using System.Collections.Concurrent;
using IntakeService.Models;
namespace IntakeService.Services;

public sealed class InMemoryDashboardMetricsStore : IDashboardMetricsStore
{
    private readonly object sync = new();
    private DashboardMetrics metrics = DashboardMetrics.Empty;
    private readonly List<RecentActivityItem> recentActivity = [];
    private readonly HashSet<string> processedEvents = new(StringComparer.Ordinal);

    public Task<DashboardReadModel> GetReadModelAsync()
    {
        lock (sync)
        {
            return Task.FromResult(new DashboardReadModel(metrics, recentActivity.ToArray()));
        }
    }

    public Task ApplyEventAsync(MetricsRecordRequest request)
    {
        var processedKey = DashboardMetricsProjector.BuildProcessedEventKey(
            request.TrackingId,
            request.EventType);

        lock (sync)
        {
            if (!processedEvents.Add(processedKey))
            {
                return Task.CompletedTask;
            }

            metrics = DashboardMetricsProjector.ApplyEvent(metrics, request);
            var updatedRecentActivity = DashboardMetricsProjector.PrependRecentActivity(
                recentActivity,
                request);
            recentActivity.Clear();
            recentActivity.AddRange(updatedRecentActivity);
        }

        return Task.CompletedTask;
    }
}
