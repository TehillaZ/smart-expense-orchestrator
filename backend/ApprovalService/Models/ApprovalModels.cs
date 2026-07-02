namespace ApprovalService.Models;

public sealed record ApprovalDecisionRequest(
    string? Decision,
    string? Comment,
    string? CorrelationId);

public sealed record ApprovalDecisionResponse(
    string TrackingId,
    string Status,
    string CorrelationId,
    string Reason);

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
    string Reason,
    string? AiRecommendation = null,
    decimal? AiConfidence = null,
    IReadOnlyList<string>? CitedRules = null,
    string? PolicyVersion = null);
