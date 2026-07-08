using ApprovalFlow.Reliability.Outbox;
using PaymentService.Models;

namespace PaymentService.Services;

public interface IPaymentOutcomePublisher
{
    Task PublishCompletedAsync(PaymentCompletedEvent paymentEvent);

    Task PublishFailedAsync(PaymentFailedEvent paymentEvent);
}

public sealed class DaprPaymentOutcomePublisher : IPaymentOutcomePublisher
{
    private const string PubSubName = "order-pubsub";

    private readonly IOutboxService outboxService;
    private readonly ILogger<DaprPaymentOutcomePublisher> logger;

    public DaprPaymentOutcomePublisher(IOutboxService outboxService, ILogger<DaprPaymentOutcomePublisher> logger)
    {
        this.outboxService = outboxService;
        this.logger = logger;
    }

    public Task PublishCompletedAsync(PaymentCompletedEvent paymentEvent)
    {
        return outboxService.EnqueueAsync(
            $"payment.completed:{paymentEvent.TrackingId}",
            PubSubName,
            PaymentOutboxTopics.PaymentCompleted,
            paymentEvent);
    }

    public Task PublishFailedAsync(PaymentFailedEvent paymentEvent)
    {
        return outboxService.EnqueueAsync(
            $"payment.failed:{paymentEvent.TrackingId}",
            PubSubName,
            PaymentOutboxTopics.PaymentFailed,
            paymentEvent);
    }
}
