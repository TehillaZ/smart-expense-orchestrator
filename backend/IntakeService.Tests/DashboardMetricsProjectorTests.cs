using IntakeService.Models;
using IntakeService.Services;

namespace IntakeService.Tests;

public class DashboardMetricsProjectorTests
{
    [Fact]
    public void ToReportingSummary_ComputesRatesFromPreAggregatedMetrics()
    {
        var readModel = new DashboardReadModel(
            new DashboardMetrics(
                TotalSubmissions: 4,
                AutoApprovedCount: 1,
                EscalatedCount: 1,
                HumanApprovedCount: 1,
                RejectedCount: 1,
                PaidCount: 1,
                PaymentFailedCount: 0,
                AutoApprovedAmount: 100m,
                HumanApprovedAmount: 450m),
            []);

        var summary = DashboardMetricsProjector.ToReportingSummary(readModel);

        Assert.Equal(4, summary.TotalSubmissions);
        Assert.Equal(100m, summary.AutoApprovedAmount);
        Assert.Equal(450m, summary.HumanApprovedAmount);
        Assert.Equal(0.25m, summary.AutoApprovalRate);
        Assert.Equal(0.25m, summary.EscalationRate);
    }

    [Fact]
    public void ApplySubmissionCreated_ResetsDailyCountOnNewUtcDay()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))
            .ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var current = new DashboardMetrics(
            TotalSubmissions: 3,
            AutoApprovedCount: 0,
            EscalatedCount: 0,
            HumanApprovedCount: 0,
            RejectedCount: 0,
            PaidCount: 0,
            PaymentFailedCount: 0,
            AutoApprovedAmount: 0m,
            HumanApprovedAmount: 0m,
            SubmissionsTodayCount: 2,
            SubmissionsTodayUtcDate: yesterday);

        var updated = DashboardMetricsProjector.ApplyEvent(
            current,
            new MetricsRecordRequest(
                "tracking-4",
                "correlation-4",
                MetricsEventTypes.SubmissionCreated,
                "Received",
                50m,
                "Received",
                "Acme Corp",
                "INV-1004"));

        Assert.Equal(4, updated.TotalSubmissions);
        Assert.Equal(1, updated.SubmissionsTodayCount);
    }

    [Fact]
    public async Task ApplyEvent_IsIdempotentForDuplicateEvents()
    {
        var store = new InMemoryDashboardMetricsStore();
        var request = new MetricsRecordRequest(
            "tracking-1",
            "correlation-1",
            MetricsEventTypes.AutoApproved,
            "AutoApproved",
            100m,
            "AutoApproved",
            "Acme Corp",
            "INV-1001");

        await store.ApplyEventAsync(request);
        await store.ApplyEventAsync(request);

        var readModel = await store.GetReadModelAsync();

        Assert.Equal(1, readModel.Metrics.AutoApprovedCount);
        Assert.Equal(100m, readModel.Metrics.AutoApprovedAmount);
    }

    [Fact]
    public async Task ApplyEvent_UpdatesRecentActivityWithLatestSubmissionFirst()
    {
        var store = new InMemoryDashboardMetricsStore();

        await store.ApplyEventAsync(new MetricsRecordRequest(
            "tracking-1",
            "correlation-1",
            MetricsEventTypes.SubmissionCreated,
            "Received",
            100m,
            "Received",
            "Acme Corp",
            "INV-1001"));

        await store.ApplyEventAsync(new MetricsRecordRequest(
            "tracking-2",
            "correlation-2",
            MetricsEventTypes.SubmissionCreated,
            "Received",
            200m,
            "Received",
            "Beta LLC",
            "INV-1002"));

        var readModel = await store.GetReadModelAsync();

        Assert.Equal(2, readModel.Metrics.TotalSubmissions);
        Assert.Equal("tracking-2", readModel.RecentActivity[0].TrackingId);
        Assert.Equal("tracking-1", readModel.RecentActivity[1].TrackingId);
    }
}
