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
    private const string PaymentCompletedTopic = "payment.completed";
    private const string PaymentFailedTopic = "payment.failed";

    private readonly Dapr.Client.DaprClient daprClient;

    public DaprPaymentOutcomePublisher(Dapr.Client.DaprClient daprClient)
    {
        this.daprClient = daprClient;
    }

    public Task PublishCompletedAsync(PaymentCompletedEvent paymentEvent)
    {
        return daprClient.PublishEventAsync(PubSubName, PaymentCompletedTopic, paymentEvent);
    }

    public Task PublishFailedAsync(PaymentFailedEvent paymentEvent)
    {
        return daprClient.PublishEventAsync(PubSubName, PaymentFailedTopic, paymentEvent);
    }
}
