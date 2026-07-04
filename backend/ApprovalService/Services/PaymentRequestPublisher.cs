using ApprovalService.Models;
using Dapr.Client;

namespace ApprovalService.Services;

public interface IPaymentRequestPublisher
{
    Task PublishAsync(SubmissionState state);
}

public sealed class DaprPaymentRequestPublisher : IPaymentRequestPublisher
{
    private const string PubSubName = "order-pubsub";
    private const string PaymentRequestedTopic = "payment.requested";

    private readonly DaprClient daprClient;
    private readonly ILogger<DaprPaymentRequestPublisher> logger;

    public DaprPaymentRequestPublisher(DaprClient daprClient, ILogger<DaprPaymentRequestPublisher> logger)
    {
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public async Task PublishAsync(SubmissionState state)
    {
        var paymentEvent = new PaymentRequestedEvent(
            state.TrackingId,
            state.CorrelationId,
            state.Vendor,
            state.InvoiceNumber,
            state.Amount,
            state.Category,
            SubmissionIdempotencyKeys.Build(state.Vendor, state.InvoiceNumber, state.Amount));

        try
        {
            await daprClient.PublishEventAsync(PubSubName, PaymentRequestedTopic, paymentEvent);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to publish {Topic} for submission {TrackingId}.",
                PaymentRequestedTopic,
                state.TrackingId);
        }

        try
        {
            await daprClient.InvokeMethodAsync(
                HttpMethod.Post,
                DaprServiceIds.PaymentService,
                "internal/payments/process",
                paymentEvent);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to invoke direct payment processing for submission {TrackingId}.",
                state.TrackingId);
        }

        logger.LogInformation(
            "Requested payment processing for submission {TrackingId} with correlation {CorrelationId}.",
            state.TrackingId,
            state.CorrelationId);
    }
}

public sealed record PaymentRequestedEvent(
    string TrackingId,
    string CorrelationId,
    string Vendor,
    string InvoiceNumber,
    decimal Amount,
    string Category,
    string IdempotencyKey);
