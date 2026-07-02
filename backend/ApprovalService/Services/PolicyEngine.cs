using ApprovalService.Models;

namespace ApprovalService.Services;

public sealed class PolicyEngine
{
    public PolicyEvaluationResult Evaluate(
        SubmissionReceivedEvent submission,
        PolicySettings settings,
        AiRecommendationResult aiRecommendation)
    {
        var appliedRules = new List<string>();
        var hardStop = false;

        if (submission.Amount > settings.AutonomyCeiling)
        {
            appliedRules.Add("AUTONOMY-CEILING");
            hardStop = true;
        }

        if (IsCategory(submission.Category, "hardware") && submission.Amount > 1000m)
        {
            appliedRules.Add("HW-02");
            hardStop = true;
        }

        if (IsCategory(submission.Category, "travel") && submission.Amount > 1500m)
        {
            appliedRules.Add("TRAVEL-02");
            hardStop = true;
        }

        if (IsCategory(submission.Category, "saas") && submission.Amount > 200m)
        {
            appliedRules.Add("SAAS-01");
            hardStop = true;
        }

        if (IsCategory(submission.Category, "meals") && submission.Amount > 500m)
        {
            appliedRules.Add("MEAL-02");
            hardStop = true;
        }

        var aiSaysApprove = string.Equals(aiRecommendation.Recommendation, "Approve", StringComparison.OrdinalIgnoreCase);
        var confidenceMet = aiRecommendation.Confidence >= settings.AutonomyConfidence;

        if (hardStop || !aiSaysApprove || !confidenceMet)
        {
            var reason = BuildEscalationReason(appliedRules, aiRecommendation, confidenceMet, aiSaysApprove);
            return new PolicyEvaluationResult(
                ApprovalStatuses.Escalated,
                reason,
                appliedRules,
                aiRecommendation);
        }

        return new PolicyEvaluationResult(
            ApprovalStatuses.AutoApproved,
            "Expense is within policy limits and eligible for autonomous approval.",
            appliedRules,
            aiRecommendation);
    }

    private static string BuildEscalationReason(
        IReadOnlyList<string> appliedRules,
        AiRecommendationResult aiRecommendation,
        bool confidenceMet,
        bool aiSaysApprove)
    {
        if (appliedRules.Count > 0)
        {
            return $"Escalated for human review due to policy rules: {string.Join(", ", appliedRules)}.";
        }

        if (!aiSaysApprove)
        {
            return $"Escalated because AI recommendation was {aiRecommendation.Recommendation}.";
        }

        if (!confidenceMet)
        {
            return $"Escalated because AI confidence {aiRecommendation.Confidence:0.00} is below the required threshold.";
        }

        return "Escalated for human review.";
    }

    private static bool IsCategory(string category, string expected)
    {
        return string.Equals(category.Trim(), expected, StringComparison.OrdinalIgnoreCase);
    }
}
