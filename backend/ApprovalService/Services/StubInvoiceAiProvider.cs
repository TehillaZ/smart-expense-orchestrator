using ApprovalService.Models;

namespace ApprovalService.Services;

public sealed class StubInvoiceAiProvider : IInvoiceAiProvider
{
    public Task<AiRecommendationResult> GetRecommendationAsync(
        SubmissionReceivedEvent submission,
        CancellationToken cancellationToken = default)
    {
        var category = submission.Category.Trim();
        var citedRules = new List<string>();
        var confidence = 0.95m;
        var recommendation = "Approve";
        var reasoning = "Stub provider: submission appears routine and within typical limits.";

        if (submission.Amount > 400m)
        {
            recommendation = "Escalate";
            confidence = 0.88m;
            citedRules.Add("AUTONOMY-CEILING");
            reasoning = "Stub provider: amount exceeds typical autonomous approval range.";
        }
        else if (string.Equals(category, "other", StringComparison.OrdinalIgnoreCase))
        {
            recommendation = "Escalate";
            confidence = 0.72m;
            citedRules.Add("AUTONOMY-CONFIDENCE");
            reasoning = "Stub provider: ambiguous category requires human review.";
        }
        else if (string.Equals(category, "hardware", StringComparison.OrdinalIgnoreCase) && submission.Amount > 1000m)
        {
            recommendation = "Escalate";
            confidence = 0.99m;
            citedRules.Add("HW-02");
            reasoning = "Stub provider: capital hardware requires human approval.";
        }
        else
        {
            if (string.Equals(category, "saas", StringComparison.OrdinalIgnoreCase))
            {
                citedRules.Add("SAAS-01");
            }
            else if (string.Equals(category, "travel", StringComparison.OrdinalIgnoreCase))
            {
                citedRules.Add("TRAVEL-01");
            }
            else if (string.Equals(category, "meals", StringComparison.OrdinalIgnoreCase))
            {
                citedRules.Add("MEAL-01");
            }
        }

        var result = new AiRecommendationResult(
            recommendation,
            confidence,
            citedRules,
            reasoning);

        return Task.FromResult(result);
    }
}
