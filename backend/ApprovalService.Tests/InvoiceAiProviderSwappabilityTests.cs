using ApprovalService.Models;
using ApprovalService.Services;

namespace ApprovalService.Tests;

public class InvoiceAiProviderSwappabilityTests
{
    [Fact]
    public async Task CustomProvider_CanReplaceStubWithoutChangingPolicyEngine()
    {
        IInvoiceAiProvider provider = new AlwaysEscalateAiProvider();
        var engine = new PolicyEngine();
        var submission = new SubmissionReceivedEvent(
            "tracking-1",
            "correlation-1",
            "Acme Corp",
            "INV-1001",
            100m,
            "meals");

        var recommendation = await provider.GetRecommendationAsync(submission);
        var result = engine.Evaluate(submission, PolicySettings.Default, recommendation);

        Assert.Equal("Escalate", recommendation.Recommendation);
        Assert.Equal(ApprovalStatuses.Escalated, result.FinalStatus);
    }

    private sealed class AlwaysEscalateAiProvider : IInvoiceAiProvider
    {
        public Task<AiRecommendationResult> GetRecommendationAsync(
            SubmissionReceivedEvent submission,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AiRecommendationResult(
                "Escalate",
                0.40m,
                ["TEST-RULE"],
                "Test provider always escalates."));
        }
    }
}
