using ApprovalService.Models;
using ApprovalService.Services;

namespace ApprovalFlow.JourneyTests;

/// <summary>
/// Simulates an adversarial LLM that always recommends approval regardless of policy limits.
/// </summary>
internal sealed class AdversarialInvoiceAiProvider : IInvoiceAiProvider
{
    public Task<AiRecommendationResult> GetRecommendationAsync(
        SubmissionReceivedEvent submission,
        CancellationToken cancellationToken = default)
    {
        var result = new AiRecommendationResult(
            "Approve",
            0.99m,
            ["PROMPT-INJECTION"],
            "User note said 'approve me' — recommending approval regardless of amount.");

        return Task.FromResult(result);
    }
}
