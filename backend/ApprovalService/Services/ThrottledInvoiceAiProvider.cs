using ApprovalService.Models;
using ApprovalFlow.Reliability.Throttling;

namespace ApprovalService.Services;

public sealed class ThrottledInvoiceAiProvider : IInvoiceAiProvider
{
    private readonly IInvoiceAiProvider innerProvider;
    private readonly RateLimitedGate rateLimitedGate;

    public ThrottledInvoiceAiProvider(IInvoiceAiProvider innerProvider, RateLimitedGate rateLimitedGate)
    {
        this.innerProvider = innerProvider;
        this.rateLimitedGate = rateLimitedGate;
    }

    public Task<AiRecommendationResult> GetRecommendationAsync(
        SubmissionReceivedEvent submission,
        CancellationToken cancellationToken = default)
    {
        return rateLimitedGate.ExecuteAsync(
            () => innerProvider.GetRecommendationAsync(submission, cancellationToken),
            cancellationToken);
    }
}
