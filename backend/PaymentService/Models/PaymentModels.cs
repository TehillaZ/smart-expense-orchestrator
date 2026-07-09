namespace PaymentService.Models;

public sealed record PaymentRequestedEvent(
    string TrackingId,
    string CorrelationId,
    string Vendor,
    string InvoiceNumber,
    decimal Amount,
    string Category,
    string IdempotencyKey,
    string? TraceParent = null);

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

public sealed record SubmissionWorkflowState(
    string TrackingId,
    string CorrelationId,
    string Vendor,
    string InvoiceNumber,
    decimal Amount,
    string Category,
    string Status,
    string Reason,
    string? AiRecommendation = null,
    decimal? AiConfidence = null,
    IReadOnlyList<string>? CitedRules = null,
    string? PolicyVersion = null,
    string? HumanDecision = null,
    string? HumanDecisionComment = null);

public sealed record PaymentSagaState(
    string TrackingId,
    string IdempotencyKey,
    string CorrelationId,
    decimal Amount,
    string InvoiceNumber,
    string Phase,
    string Status,
    string Reason,
    string? ReservationId,
    DateTimeOffset UpdatedAt)
{
    public bool IsTerminal =>
        string.Equals(Status, PaymentStatuses.Paid, StringComparison.Ordinal)
        || string.Equals(Status, PaymentStatuses.PaymentFailed, StringComparison.Ordinal)
        || string.Equals(Status, PaymentStatuses.Compensated, StringComparison.Ordinal);
}

public sealed record PaymentReservationResult(
    bool Success,
    string? ReservationId,
    string Reason);

public sealed record PaymentExecutionResult(
    bool Success,
    string Reason);

public sealed record PaymentCompensationResult(
    bool Success,
    string Reason);
