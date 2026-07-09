using ApprovalFlow.TestSupport;
using ApprovalService.Models;
using ApprovalService.Services;
using IntakeService.Models;
using IntakeService.Services;

namespace ApprovalFlow.IntegrationTests;

/// <summary>
/// Locally runnable integration tests for state-store contracts used by Dapr-backed services.
/// These exercise the same interfaces as production stores without requiring Redis/Dapr sidecars.
/// </summary>
[Trait(TestLayers.TraitName, TestLayers.Integration)]
public class StateStoreContractTests
{
    [Fact]
    public async Task IntakeSubmissionStore_PersistsAndResolvesIdempotentDuplicates()
    {
        ISubmissionStore store = new InMemorySubmissionStore();
        var request = new SubmissionRequest("Acme Corp", "INV-N6-01", 120m, "meals");

        var first = await store.CreateSubmissionAsync(request, "corr-1");
        var duplicate = await store.CreateSubmissionAsync(request, "corr-2");
        var fetched = await store.GetSubmissionAsync(first.Submission.TrackingId);

        Assert.False(first.IsDuplicate);
        Assert.True(duplicate.IsDuplicate);
        Assert.Equal(first.Submission.TrackingId, duplicate.Submission.TrackingId);
        Assert.NotNull(fetched);
        Assert.Equal(first.Submission.TrackingId, fetched!.TrackingId);
    }

    [Fact]
    public async Task ApprovalSubmissionStateStore_PersistsEscalationQueueMembership()
    {
        ISubmissionStateStore store = new InMemoryApprovalStateStore();
        var state = new SubmissionState(
            "tracking-n6",
            "corr-n6",
            "Acme Corp",
            "INV-N6-02",
            500m,
            "meals",
            ApprovalStatuses.Escalated,
            "Escalated for human review.");

        await store.SaveAsync(state);
        await store.AddToEscalationQueueAsync(state.TrackingId);

        var loaded = await store.GetAsync(state.TrackingId);
        var queue = await store.GetEscalatedSubmissionsAsync();

        Assert.NotNull(loaded);
        Assert.Equal(ApprovalStatuses.Escalated, loaded!.Status);
        Assert.Contains(queue, item => item.TrackingId == state.TrackingId);

        await store.RemoveFromEscalationQueueAsync(state.TrackingId);
        var afterRemove = await store.GetEscalatedSubmissionsAsync();
        Assert.DoesNotContain(afterRemove, item => item.TrackingId == state.TrackingId);
    }

    [Fact]
    public async Task PolicySettingsStore_RoundTripsAutonomyThresholds()
    {
        IPolicySettingsStore store = new InMemoryPolicySettingsStore();
        var updated = PolicySettings.Default with
        {
            AutonomyCeiling = 350m,
            AutonomyConfidence = 0.95m,
            PolicyVersion = "v1-n6",
        };

        await store.SaveAsync(updated);
        var loaded = await store.GetAsync();

        Assert.Equal(350m, loaded.AutonomyCeiling);
        Assert.Equal(0.95m, loaded.AutonomyConfidence);
        Assert.Equal("v1-n6", loaded.PolicyVersion);
    }

    private sealed class InMemoryApprovalStateStore : ISubmissionStateStore
    {
        private readonly Dictionary<string, SubmissionState> states = new(StringComparer.Ordinal);
        private readonly HashSet<string> escalationQueue = new(StringComparer.Ordinal);

        public Task<SubmissionState?> GetAsync(string trackingId)
        {
            states.TryGetValue(trackingId, out var state);
            return Task.FromResult(state);
        }

        public Task SaveAsync(SubmissionState state)
        {
            states[state.TrackingId] = state;
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

    private sealed class InMemoryPolicySettingsStore : IPolicySettingsStore
    {
        private PolicySettings current = PolicySettings.Default;

        public Task<PolicySettings> GetAsync() => Task.FromResult(current);

        public Task SaveAsync(PolicySettings settings)
        {
            current = settings;
            return Task.CompletedTask;
        }
    }
}
