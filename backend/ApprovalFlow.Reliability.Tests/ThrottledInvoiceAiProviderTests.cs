using ApprovalService.Models;
using ApprovalService.Services;
using ApprovalFlow.Reliability.Throttling;

namespace ApprovalFlow.Reliability.Tests;

public class ThrottledInvoiceAiProviderTests
{
    [Fact]
    public async Task GetRecommendationAsync_EnforcesRateLimit()
    {
        using var rateLimitedGate = new RateLimitedGate(permitLimit: 1, window: TimeSpan.FromSeconds(30));
        var provider = new ThrottledInvoiceAiProvider(new SlowAiProvider(), rateLimitedGate);
        var submission = new SubmissionReceivedEvent(
            "tracking-1",
            "correlation-1",
            "Acme",
            "INV-1",
            100m,
            "meals");

        await provider.GetRecommendationAsync(submission);
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetRecommendationAsync(submission));
    }

    private sealed class SlowAiProvider : IInvoiceAiProvider
    {
        public Task<AiRecommendationResult> GetRecommendationAsync(
            SubmissionReceivedEvent submission,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AiRecommendationResult("Approve", 0.9m, ["RULE-1"], "Stub reasoning."));
        }
    }
}
