using ApprovalService.Models;
using Dapr.Client;

namespace ApprovalService.Services;

public sealed class DaprSubmissionStateStore : ISubmissionStateStore
{
    private const string StateStoreName = "approval-state";
    private const string EscalationQueueKey = "escalation-queue-index";

    private readonly DaprClient daprClient;

    public DaprSubmissionStateStore(DaprClient daprClient)
    {
        this.daprClient = daprClient;
    }

    public async Task<SubmissionState?> GetAsync(string trackingId)
    {
        return await daprClient.GetStateAsync<SubmissionState>(StateStoreName, trackingId);
    }

    public Task SaveAsync(SubmissionState state)
    {
        return daprClient.SaveStateAsync(StateStoreName, state.TrackingId, state);
    }

    public async Task<IReadOnlyList<SubmissionState>> GetEscalatedSubmissionsAsync()
    {
        var index = await GetQueueIndexAsync();
        var submissions = new List<SubmissionState>();

        foreach (var trackingId in index)
        {
            var state = await GetAsync(trackingId);
            if (state is not null && string.Equals(state.Status, ApprovalStatuses.Escalated, StringComparison.Ordinal))
            {
                submissions.Add(state);
            }
        }

        return submissions;
    }

    public async Task AddToEscalationQueueAsync(string trackingId)
    {
        var index = await GetQueueIndexAsync();
        if (index.Contains(trackingId))
        {
            return;
        }

        index.Add(trackingId);
        await SaveQueueIndexAsync(index);
    }

    public async Task RemoveFromEscalationQueueAsync(string trackingId)
    {
        var index = await GetQueueIndexAsync();
        if (index.Remove(trackingId))
        {
            await SaveQueueIndexAsync(index);
        }
    }

    private async Task<List<string>> GetQueueIndexAsync()
    {
        var index = await daprClient.GetStateAsync<List<string>>(StateStoreName, EscalationQueueKey);
        return index ?? [];
    }

    private Task SaveQueueIndexAsync(List<string> trackingIds)
    {
        return daprClient.SaveStateAsync(StateStoreName, EscalationQueueKey, trackingIds);
    }
}
