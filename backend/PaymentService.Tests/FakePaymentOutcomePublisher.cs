using PaymentService.Models;
using PaymentService.Services;

namespace PaymentService.Tests;

internal sealed class FakePaymentOutcomePublisher : IPaymentOutcomePublisher
{
    public PaymentCompletedEvent? LastCompleted { get; private set; }

    public PaymentFailedEvent? LastFailed { get; private set; }

    public Task PublishCompletedAsync(PaymentCompletedEvent paymentEvent)
    {
        LastCompleted = paymentEvent;
        return Task.CompletedTask;
    }

    public Task PublishFailedAsync(PaymentFailedEvent paymentEvent)
    {
        LastFailed = paymentEvent;
        return Task.CompletedTask;
    }
}
