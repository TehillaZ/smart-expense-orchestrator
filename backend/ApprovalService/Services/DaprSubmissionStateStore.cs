using System.Text.Json;
using ApprovalService.Models;
using Dapr.Client;

namespace ApprovalService.Services;

public sealed class DaprSubmissionStateStore : ISubmissionStateStore
{
    private const string StateStoreName = "approval-state";
    private const string EscalationQueueKey = "escalation-queue-index";

    private readonly DaprClient daprClient;
    private readonly ILogger<DaprSubmissionStateStore> logger;

    public DaprSubmissionStateStore(DaprClient daprClient, ILogger<DaprSubmissionStateStore> logger)
    {
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public async Task<SubmissionState?> GetAsync(string trackingId)
    {
        try
        {
            var state = await daprClient.GetStateAsync<SubmissionState>(StateStoreName, trackingId);
            if (state is not null)
            {
                return state;
            }

            return await TryReadCompatibleStateAsync(trackingId);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(
                ex,
                "Primary deserialization failed for submission {TrackingId}. Attempting compatible read.",
                trackingId);
            return await TryReadCompatibleStateAsync(trackingId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read submission {TrackingId} from state store.", trackingId);
            throw;
        }
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

    private async Task<SubmissionState?> TryReadCompatibleStateAsync(string trackingId)
    {
        try
        {
            var rawState = await daprClient.GetStateAsync<JsonElement>(StateStoreName, trackingId);
            if (rawState.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                return null;
            }

            return JsonSerializer.Deserialize<SubmissionState>(rawState.GetRawText());
        }
        catch (JsonException ex)
        {
            logger.LogWarning(
                ex,
                "Compatible deserialization failed for submission {TrackingId}. Treating as missing approval state.",
                trackingId);
            return null;
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
