using PaymentService.Models;
using PaymentService.Services;

namespace PaymentService.Tests;

internal sealed class FakePaymentSagaStore : IPaymentSagaStore
{
    private readonly Dictionary<string, PaymentSagaState> states = new(StringComparer.Ordinal);

    public PaymentSagaState? LastSaved { get; private set; }

    public void Seed(PaymentSagaState state) => states[state.IdempotencyKey] = state;

    public Task<PaymentSagaState?> GetAsync(string idempotencyKey)
    {
        states.TryGetValue(idempotencyKey, out var state);
        return Task.FromResult(state);
    }

    public Task SaveAsync(PaymentSagaState state)
    {
        states[state.IdempotencyKey] = state;
        LastSaved = state;
        return Task.CompletedTask;
    }
}

internal sealed class FakeSubmissionWorkflowStore : ISubmissionWorkflowStore
{
    private readonly Dictionary<string, SubmissionWorkflowState> states = new(StringComparer.Ordinal);

    public SubmissionWorkflowState? LastSaved { get; private set; }

    public void Seed(SubmissionWorkflowState state) => states[state.TrackingId] = state;

    public Task<SubmissionWorkflowState?> GetAsync(string trackingId)
    {
        states.TryGetValue(trackingId, out var state);
        return Task.FromResult(state);
    }

    public Task SaveAsync(SubmissionWorkflowState state)
    {
        states[state.TrackingId] = state;
        LastSaved = state;
        return Task.CompletedTask;
    }
}
