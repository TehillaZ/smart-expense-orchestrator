using ApprovalService.Models;
using ApprovalService.Services;

namespace ApprovalFlow.PolicyRag.Tests;

public class RagStubInvoiceAiProviderTests
{
    [Fact]
    public async Task GetRecommendationAsync_IncludesRetrievedClausesInPrompt()
    {
        var provider = new StubInvoiceAiProvider(PolicyRagTestSupport.CreateRetriever());
        var submission = new SubmissionReceivedEvent(
            "tracking-1",
            "correlation-1",
            "Acme",
            "INV-1",
            60m,
            "meals");

        var result = await provider.GetRecommendationAsync(submission);

        Assert.Contains("Retrieved policy clauses:", result.ReasoningSummary, StringComparison.Ordinal);
        Assert.Contains("MEAL-01", result.ReasoningSummary, StringComparison.Ordinal);
        Assert.Contains("MEAL-01", result.CitedRules);
    }

    [Fact]
    public async Task GetRecommendationAsync_WithAmountAboveCeiling_StillEscalatesViaPolicyEngineGuard()
    {
        var provider = new StubInvoiceAiProvider(PolicyRagTestSupport.CreateRetriever());
        var submission = new SubmissionReceivedEvent(
            "tracking-1",
            "correlation-1",
            "Acme",
            "INV-1",
            500m,
            "meals");

        var aiResult = await provider.GetRecommendationAsync(submission);
        var engine = new PolicyEngine();
        var evaluation = engine.Evaluate(submission, PolicySettings.Default, aiResult);

        Assert.Equal(ApprovalStatuses.Escalated, evaluation.FinalStatus);
        Assert.Contains("AUTONOMY-CEILING", evaluation.AppliedRules);
    }
}
