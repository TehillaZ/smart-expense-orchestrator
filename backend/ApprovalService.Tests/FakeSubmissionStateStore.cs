using ApprovalService.Models;
using ApprovalService.Services;

namespace ApprovalService.Tests;

internal sealed class FakeSubmissionStateStore : ISubmissionStateStore
{
    private readonly Dictionary<string, SubmissionState> states = new();

    public SubmissionState? LastSaved { get; private set; }

    public void Seed(SubmissionState state) => states[state.TrackingId] = state;

    public Task<SubmissionState?> GetAsync(string trackingId)
    {
        states.TryGetValue(trackingId, out var state);
        return Task.FromResult(state);
    }

    public Task SaveAsync(SubmissionState state)
    {
        states[state.TrackingId] = state;
        LastSaved = state;
        return Task.CompletedTask;
    }
}
