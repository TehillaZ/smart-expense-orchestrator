namespace ApprovalService.Models;

public sealed record CategoryPolicyRule(
    string Category,
    decimal MaxAmount,
    string RuleId);

public sealed record PolicySettings(
    decimal AutonomyCeiling,
    decimal AutonomyConfidence,
    string PolicyVersion,
    IReadOnlyList<CategoryPolicyRule> CategoryRules)
{
    public static PolicySettings Default { get; } = new(
        400m,
        0.90m,
        "v1",
        [
            new CategoryPolicyRule("travel", 1500m, "TRAVEL-02"),
            new CategoryPolicyRule("hardware", 1000m, "HW-02"),
            new CategoryPolicyRule("saas", 200m, "SAAS-01"),
            new CategoryPolicyRule("meals", 500m, "MEAL-02"),
        ]);
}

public sealed record PolicySettingsUpdateRequest(
    decimal? AutonomyCeiling,
    decimal? AutonomyConfidence,
    IReadOnlyList<CategoryPolicyRule>? CategoryRules = null);

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
    string Reason,
    string? HumanDecision = null,
    string? HumanDecisionComment = null);
