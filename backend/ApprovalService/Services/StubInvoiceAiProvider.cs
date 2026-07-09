using ApprovalFlow.PolicyRag;
using ApprovalService.Models;

namespace ApprovalService.Services;

public sealed class StubInvoiceAiProvider : IInvoiceAiProvider
{
    private readonly IPolicyRetriever policyRetriever;

    public StubInvoiceAiProvider(IPolicyRetriever policyRetriever)
    {
        this.policyRetriever = policyRetriever;
    }

    public async Task<AiRecommendationResult> GetRecommendationAsync(
        SubmissionReceivedEvent submission,
        CancellationToken cancellationToken = default)
    {
        var retrieval = await policyRetriever.RetrieveAsync(
            submission.Category,
            submission.Amount,
            cancellationToken);

        var category = submission.Category.Trim();
        var citedRules = retrieval.Clauses.Select(clause => clause.RuleId).ToList();
        var confidence = 0.95m;
        var recommendation = "Approve";
        var reasoning = "Stub provider used retrieved policy clauses to assess the submission.";

        if (submission.Amount > 400m)
        {
            recommendation = "Escalate";
            confidence = 0.88m;
            EnsureRuleCited(citedRules, "AUTONOMY-CEILING");
            reasoning = "Retrieved autonomy ceiling indicates human review is required for this amount.";
        }
        else if (string.Equals(category, "other", StringComparison.OrdinalIgnoreCase)
            || string.Equals(category, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            recommendation = "Escalate";
            confidence = 0.72m;
            EnsureRuleCited(citedRules, "AUTONOMY-CONFIDENCE");
            reasoning = "Unknown category retrieved general fallback policy clauses and requires human review.";
        }
        else if (string.Equals(category, "hardware", StringComparison.OrdinalIgnoreCase) && submission.Amount > 1000m)
        {
            recommendation = "Escalate";
            confidence = 0.99m;
            EnsureRuleCited(citedRules, "HW-02");
            reasoning = "Retrieved hardware policy requires human approval for capital expenses.";
        }

        var promptContext = retrieval.PromptContext;
        var reasoningSummary =
            $"Retrieved policy clauses: {promptContext}. {reasoning}";

        return new AiRecommendationResult(
            recommendation,
            confidence,
            citedRules,
            reasoningSummary);
    }

    private static void EnsureRuleCited(ICollection<string> citedRules, string ruleId)
    {
        if (!citedRules.Contains(ruleId, StringComparer.OrdinalIgnoreCase))
        {
            citedRules.Add(ruleId);
        }
    }
}
