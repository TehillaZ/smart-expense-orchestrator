using PaymentService.Models;
using Dapr.Client;

namespace PaymentService.Services;

public sealed class DaprPaymentSagaStore : IPaymentSagaStore
{
    private const string StateStoreName = "approval-state";
    private const string KeyPrefix = "payment-saga";

    private readonly DaprClient daprClient;

    public DaprPaymentSagaStore(DaprClient daprClient)
    {
        this.daprClient = daprClient;
    }

    public Task<PaymentSagaState?> GetAsync(string idempotencyKey)
    {
        return daprClient.GetStateAsync<PaymentSagaState>(StateStoreName, BuildKey(idempotencyKey));
    }

    public Task SaveAsync(PaymentSagaState state)
    {
        return daprClient.SaveStateAsync(StateStoreName, BuildKey(state.IdempotencyKey), state);
    }

    private static string BuildKey(string idempotencyKey) => $"{KeyPrefix}:{idempotencyKey}";
}

public sealed class DaprSubmissionWorkflowStore : ISubmissionWorkflowStore
{
    private const string StateStoreName = "approval-state";

    private readonly DaprClient daprClient;

    public DaprSubmissionWorkflowStore(DaprClient daprClient)
    {
        this.daprClient = daprClient;
    }

    public Task<SubmissionWorkflowState?> GetAsync(string trackingId)
    {
        return daprClient.GetStateAsync<SubmissionWorkflowState>(StateStoreName, trackingId);
    }

    public Task SaveAsync(SubmissionWorkflowState state)
    {
        return daprClient.SaveStateAsync(StateStoreName, state.TrackingId, state);
    }
}
