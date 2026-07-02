namespace ApprovalService.Models;

public sealed record ApprovalDecisionRequest(
    string? Decision,
    string? Comment,
    string? CorrelationId);

public sealed record ApprovalDecisionResponse(
    string TrackingId,
    string Status,
    string CorrelationId);

public sealed record ValidationErrorResponse(
    IReadOnlyList<string> Errors);

public sealed record SubmissionReceivedEvent(
    string TrackingId,
    string CorrelationId,
    string Vendor,
    string InvoiceNumber,
    decimal Amount,
    string Category);

public sealed record SubmissionState(
    string TrackingId,
    string CorrelationId,
    string Vendor,
    string InvoiceNumber,
    decimal Amount,
    string Category,
    string Status,
    string Reason);
