namespace ApprovalService.Models;

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
    public const string AutoApproved = "AutoApproved";
    public const string Escalated = "Escalated";
    public const string HumanApproved = "HumanApproved";
    public const string Rejected = "Rejected";
}
