using ApprovalService.Models;
using ApprovalService.Services;

namespace ApprovalService.Tests;

internal sealed class FakeSubmissionStateStore : ISubmissionStateStore
{
    private readonly Dictionary<string, SubmissionState> states = new();
    private readonly HashSet<string> escalationQueue = new(StringComparer.Ordinal);

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

    public Task<IReadOnlyList<SubmissionState>> GetEscalatedSubmissionsAsync()
    {
        IReadOnlyList<SubmissionState> results = escalationQueue
            .Select(id => states.TryGetValue(id, out var state) ? state : null)
            .Where(state => state is not null && state.Status == ApprovalStatuses.Escalated)
            .Cast<SubmissionState>()
            .ToArray();

        return Task.FromResult(results);
    }

    public Task AddToEscalationQueueAsync(string trackingId)
    {
        escalationQueue.Add(trackingId);
        return Task.CompletedTask;
    }

    public Task RemoveFromEscalationQueueAsync(string trackingId)
    {
        escalationQueue.Remove(trackingId);
        return Task.CompletedTask;
    }
}

internal sealed class FakePolicySettingsStore : IPolicySettingsStore
{
    public PolicySettings Current { get; set; } = PolicySettings.Default;

    public Task<PolicySettings> GetAsync() => Task.FromResult(Current);

    public Task SaveAsync(PolicySettings settings)
    {
        Current = settings;
        return Task.CompletedTask;
    }
}
