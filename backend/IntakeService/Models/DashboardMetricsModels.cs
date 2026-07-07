namespace IntakeService.Models;

public sealed record DashboardMetrics(
    int TotalSubmissions,
    int AutoApprovedCount,
    int EscalatedCount,
    int HumanApprovedCount,
    int RejectedCount,
    int PaidCount,
    int PaymentFailedCount,
    decimal AutoApprovedAmount,
    decimal HumanApprovedAmount,
    int SubmissionsTodayCount = 0,
    string? SubmissionsTodayUtcDate = null)
{
    public static DashboardMetrics Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0m, 0m, 0, null);
}

public sealed record DashboardReadModel(
    DashboardMetrics Metrics,
    IReadOnlyList<RecentActivityItem> RecentActivity);

public sealed record MetricsRecordRequest(
    string TrackingId,
    string CorrelationId,
    string EventType,
    string Status,
    decimal Amount,
    string DecisionType,
    string Vendor,
    string InvoiceNumber);

public static class MetricsEventTypes
{
    public const string SubmissionCreated = "SubmissionCreated";
    public const string AutoApproved = "AutoApproved";
    public const string Escalated = "Escalated";
    public const string HumanApproved = "HumanApproved";
    public const string Rejected = "Rejected";
    public const string Paid = "Paid";
    public const string PaymentFailed = "PaymentFailed";
}
