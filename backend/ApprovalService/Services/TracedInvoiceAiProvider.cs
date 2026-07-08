using ApprovalFlow.Observability;
using ApprovalService.Models;

namespace ApprovalService.Services;

public sealed class TracedInvoiceAiProvider : IInvoiceAiProvider
{
    public const string ActivityName = "invoice.ai.recommendation";

    private readonly IInvoiceAiProvider innerProvider;
    private readonly ObservabilityMetrics metrics;

    public TracedInvoiceAiProvider(IInvoiceAiProvider innerProvider, ObservabilityMetrics metrics)
    {
        this.innerProvider = innerProvider;
        this.metrics = metrics;
    }

    public Task<AiRecommendationResult> GetRecommendationAsync(
        SubmissionReceivedEvent submission,
        CancellationToken cancellationToken = default)
    {
        var tags = new[]
        {
            new KeyValuePair<string, object?>("submission.tracking_id", submission.TrackingId),
            new KeyValuePair<string, object?>("submission.category", submission.Category),
            new KeyValuePair<string, object?>("submission.amount", submission.Amount),
            new KeyValuePair<string, object?>("ai.provider", "stub"),
        };

        return ActivityTracing.TraceAsync(
            ActivityName,
            () => innerProvider.GetRecommendationAsync(submission, cancellationToken),
            tags,
            metrics);
    }
}
