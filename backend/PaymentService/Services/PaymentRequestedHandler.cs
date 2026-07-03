using PaymentService.Models;

namespace PaymentService.Services;

public sealed class PaymentRequestedHandler
{
    private readonly PaymentSagaProcessor sagaProcessor;

    public PaymentRequestedHandler(PaymentSagaProcessor sagaProcessor)
    {
        this.sagaProcessor = sagaProcessor;
    }

    public Task HandleAsync(PaymentRequestedEvent paymentEvent)
    {
        return sagaProcessor.ProcessAsync(paymentEvent);
    }
}
