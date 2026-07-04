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
    private readonly ILogger<DaprPaymentOutcomePublisher> logger;

    public DaprPaymentOutcomePublisher(
        Dapr.Client.DaprClient daprClient,
        ILogger<DaprPaymentOutcomePublisher> logger)
    {
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public async Task PublishCompletedAsync(PaymentCompletedEvent paymentEvent)
    {
        try
        {
            await daprClient.PublishEventAsync(PubSubName, PaymentCompletedTopic, paymentEvent);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to publish {Topic} for submission {TrackingId}.",
                PaymentCompletedTopic,
                paymentEvent.TrackingId);
        }

        try
        {
            await daprClient.InvokeMethodAsync(
                HttpMethod.Post,
                DaprServiceIds.NotificationService,
                "internal/notifications/payment-completed",
                paymentEvent);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to invoke direct payment completion notification for submission {TrackingId}.",
                paymentEvent.TrackingId);
        }
    }

    public async Task PublishFailedAsync(PaymentFailedEvent paymentEvent)
    {
        try
        {
            await daprClient.PublishEventAsync(PubSubName, PaymentFailedTopic, paymentEvent);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to publish {Topic} for submission {TrackingId}.",
                PaymentFailedTopic,
                paymentEvent.TrackingId);
        }

        try
        {
            await daprClient.InvokeMethodAsync(
                HttpMethod.Post,
                DaprServiceIds.NotificationService,
                "internal/notifications/payment-failed",
                paymentEvent);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to invoke direct payment failure notification for submission {TrackingId}.",
                paymentEvent.TrackingId);
        }
    }
}
