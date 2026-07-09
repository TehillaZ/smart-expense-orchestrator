using ApprovalFlow.Observability;
using ApprovalFlow.Reliability.Outbox;
using ApprovalService.Models;

namespace ApprovalService.Services;

public interface IPaymentRequestPublisher
{
    Task PublishAsync(SubmissionState state);
}

public sealed class DaprPaymentRequestPublisher : IPaymentRequestPublisher
{
    private const string PubSubName = "order-pubsub";
    private const string PaymentRequestedTopic = "payment.requested";

    private readonly IOutboxService outboxService;
    private readonly ILogger<DaprPaymentRequestPublisher> logger;

    public DaprPaymentRequestPublisher(IOutboxService outboxService, ILogger<DaprPaymentRequestPublisher> logger)
    {
        this.outboxService = outboxService;
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
            SubmissionIdempotencyKeys.Build(state.Vendor, state.InvoiceNumber, state.Amount),
            TraceContextPropagator.CaptureCurrent());

        var idempotencyKey = $"payment.requested:{state.TrackingId}";
        await outboxService.EnqueueAsync(
            idempotencyKey,
            PubSubName,
            PaymentRequestedTopic,
            paymentEvent);

        logger.LogInformation(
            "Queued payment.requested outbox message for submission {TrackingId} with correlation {CorrelationId}.",
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
    string IdempotencyKey,
    string? TraceParent = null);
