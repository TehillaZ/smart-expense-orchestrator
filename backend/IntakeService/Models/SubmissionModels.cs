namespace IntakeService.Models;

public sealed record SubmissionRequest(
    string? Vendor,
    string? InvoiceNumber,
    decimal Amount,
    string? Category);

public sealed record SubmissionAcceptedResponse(
    string TrackingId,
    string CorrelationId,
    bool IsDuplicate = false);

public sealed record SubmissionCreationResult(
    SubmissionStatusResponse Submission,
    bool IsDuplicate);

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
    string? PolicyVersion = null,
    decimal? AutonomyCeilingAtDecision = null,
    string? HumanDecision = null,
    string? HumanDecisionComment = null,
    string? HumanDecisionActor = null,
    int DuplicateResubmitCount = 0);

public sealed record ValidationErrorResponse(
    IReadOnlyList<string> Errors);

public sealed record SubmissionReceivedEvent(
    string TrackingId,
    string CorrelationId,
    string Vendor,
    string InvoiceNumber,
    decimal Amount,
    string Category,
    string? TraceParent = null);
