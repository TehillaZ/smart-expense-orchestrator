namespace ApprovalService.Models;

public sealed record PolicySettings(
    decimal AutonomyCeiling,
    decimal AutonomyConfidence,
    string PolicyVersion)
{
    public static PolicySettings Default { get; } = new(400m, 0.90m, "v1");
}

public sealed record PolicySettingsUpdateRequest(
    decimal? AutonomyCeiling,
    decimal? AutonomyConfidence);

public sealed record AiRecommendationResult(
    string Recommendation,
    decimal Confidence,
    IReadOnlyList<string> CitedRules,
    string ReasoningSummary);

public sealed record PolicyEvaluationResult(
    string FinalStatus,
    string Reason,
    IReadOnlyList<string> AppliedRules,
    AiRecommendationResult AiRecommendation);

public sealed record EscalationQueueItem(
    string TrackingId,
    string Vendor,
    string InvoiceNumber,
    decimal Amount,
    string Category,
    string AiRecommendation,
    decimal Confidence,
    IReadOnlyList<string> CitedRules,
    string Reason);
