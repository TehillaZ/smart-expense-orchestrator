using ApprovalService.Models;
using ApprovalService.Services;

namespace ApprovalService.Tests;

internal sealed class FakePaymentRequestPublisher : IPaymentRequestPublisher
{
    public List<SubmissionState> PublishedStates { get; } = [];

    public Task PublishAsync(SubmissionState state)
    {
        PublishedStates.Add(state);
        return Task.CompletedTask;
    }
}
