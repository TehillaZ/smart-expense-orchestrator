namespace IntakeService.Models;

public sealed record SubmissionRequest(
    string? Vendor,
    string? InvoiceNumber,
    decimal Amount,
    string? Category);

public sealed record SubmissionAcceptedResponse(
    string TrackingId,
    string CorrelationId);

public sealed record SubmissionStatusResponse(
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
    string? PolicyVersion = null);

public sealed record ValidationErrorResponse(
    IReadOnlyList<string> Errors);

public sealed record SubmissionReceivedEvent(
    string TrackingId,
    string CorrelationId,
    string Vendor,
    string InvoiceNumber,
    decimal Amount,
    string Category);
