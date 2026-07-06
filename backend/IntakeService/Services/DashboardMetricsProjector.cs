using IntakeService.Models;

namespace IntakeService.Services;

public static class DashboardMetricsProjector
{
    private const int RecentActivityLimit = 10;

    public static DashboardMetrics ApplyEvent(DashboardMetrics current, MetricsRecordRequest request)
    {
        return request.EventType switch
        {
            MetricsEventTypes.SubmissionCreated => current with
            {
                TotalSubmissions = current.TotalSubmissions + 1,
            },
            MetricsEventTypes.AutoApproved => current with
            {
                AutoApprovedCount = current.AutoApprovedCount + 1,
                AutoApprovedAmount = current.AutoApprovedAmount + request.Amount,
            },
            MetricsEventTypes.Escalated => current with
            {
                EscalatedCount = current.EscalatedCount + 1,
            },
            MetricsEventTypes.HumanApproved => current with
            {
                HumanApprovedCount = current.HumanApprovedCount + 1,
                HumanApprovedAmount = current.HumanApprovedAmount + request.Amount,
            },
            MetricsEventTypes.Rejected => current with
            {
                RejectedCount = current.RejectedCount + 1,
            },
            MetricsEventTypes.Paid => current with
            {
                PaidCount = current.PaidCount + 1,
            },
            MetricsEventTypes.PaymentFailed => current with
            {
                PaymentFailedCount = current.PaymentFailedCount + 1,
            },
            _ => throw new InvalidOperationException($"Unsupported metrics event type: {request.EventType}")
        };
    }

    public static IReadOnlyList<RecentActivityItem> PrependRecentActivity(
        IReadOnlyList<RecentActivityItem> current,
        MetricsRecordRequest request)
    {
        var updated = new List<RecentActivityItem>(RecentActivityLimit)
        {
            new(
                request.TrackingId,
                request.Status,
                request.Amount,
                request.DecisionType,
                request.Vendor,
                request.InvoiceNumber,
                DateTimeOffset.UtcNow),
        };

        foreach (var item in current)
        {
            if (updated.Count >= RecentActivityLimit)
            {
                break;
            }

            if (string.Equals(item.TrackingId, request.TrackingId, StringComparison.Ordinal))
            {
                continue;
            }

            updated.Add(item);
        }

        return updated;
    }

    public static ReportingSummaryResponse ToReportingSummary(DashboardReadModel readModel)
    {
        var metrics = readModel.Metrics;
        var total = metrics.TotalSubmissions;
        var autoApprovalRate = total == 0 ? 0m : Math.Round((decimal)metrics.AutoApprovedCount / total, 4);
        var escalationRate = total == 0 ? 0m : Math.Round((decimal)metrics.EscalatedCount / total, 4);

        return new ReportingSummaryResponse(
            metrics.TotalSubmissions,
            metrics.AutoApprovedCount,
            metrics.EscalatedCount,
            metrics.HumanApprovedCount,
            metrics.RejectedCount,
            metrics.PaidCount,
            metrics.PaymentFailedCount,
            metrics.AutoApprovedAmount,
            metrics.HumanApprovedAmount,
            autoApprovalRate,
            escalationRate,
            readModel.RecentActivity);
    }

    public static string BuildProcessedEventKey(string trackingId, string eventType) =>
        $"{trackingId}:{eventType}";
}
