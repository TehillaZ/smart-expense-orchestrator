using PaymentService.Models;

namespace PaymentService.Services;

public interface IPaymentSagaStore
{
    Task<PaymentSagaState?> GetAsync(string idempotencyKey);

    Task SaveAsync(PaymentSagaState state);
}

public interface ISubmissionWorkflowStore
{
    Task<SubmissionWorkflowState?> GetAsync(string trackingId);

    Task SaveAsync(SubmissionWorkflowState state);
}
