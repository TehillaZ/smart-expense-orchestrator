namespace PaymentService.Models;

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
    public const string Paid = "Paid";
    public const string PaymentFailed = "PaymentFailed";
}
