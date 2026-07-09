namespace NotificationService.Models;

public sealed record PaymentCompletedEvent(
    string TrackingId,
    string CorrelationId,
    string Status,
    string Reason,
    decimal Amount,
    string? TraceParent = null);

public sealed record PaymentFailedEvent(
    string TrackingId,
    string CorrelationId,
    string Status,
    string Reason,
    decimal Amount,
    string? TraceParent = null);

public sealed record NotificationRecord(
    string TrackingId,
    string CorrelationId,
    string Status,
    string Reason,
    decimal Amount,
    string OutcomeType,
    string Channel,
    DateTimeOffset SentAt);

public sealed record NotificationResponse(
    string TrackingId,
    string CorrelationId,
    string Status,
    string Reason,
    decimal Amount,
    string OutcomeType,
    string Channel,
    DateTimeOffset SentAt);

public static class NotificationOutcomeTypes
{
    public const string PaymentCompleted = "payment.completed";
    public const string PaymentFailed = "payment.failed";
}

public static class NotificationChannels
{
    public const string Stub = "stub";
}
