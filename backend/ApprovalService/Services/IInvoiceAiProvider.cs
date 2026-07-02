using ApprovalService.Models;

namespace ApprovalService.Services;

public interface IInvoiceAiProvider
{
    Task<AiRecommendationResult> GetRecommendationAsync(SubmissionReceivedEvent submission, CancellationToken cancellationToken = default);
}
