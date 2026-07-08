using ApprovalFlow.Reliability.Throttling;
using PaymentService.Models;

namespace PaymentService.Services;

public sealed class PaymentRequestedHandler
{
    private readonly PaymentSagaProcessor sagaProcessor;
    private readonly BulkheadGate bulkheadGate;

    public PaymentRequestedHandler(PaymentSagaProcessor sagaProcessor, BulkheadGate bulkheadGate)
    {
        this.sagaProcessor = sagaProcessor;
        this.bulkheadGate = bulkheadGate;
    }

    public Task HandleAsync(PaymentRequestedEvent paymentEvent)
    {
        return bulkheadGate.ExecuteAsync(() => sagaProcessor.ProcessAsync(paymentEvent));
    }
}
