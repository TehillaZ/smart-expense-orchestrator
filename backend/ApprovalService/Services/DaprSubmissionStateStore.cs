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

    public Task AddToEscalationQueueAsync(string trackingId)
    {
        return MutateQueueIndexAsync(index =>
        {
            if (index.Contains(trackingId))
            {
                return false;
            }

            index.Add(trackingId);
            return true;
        });
    }

    public Task RemoveFromEscalationQueueAsync(string trackingId)
    {
        return MutateQueueIndexAsync(index => index.Remove(trackingId));
    }

    private async Task MutateQueueIndexAsync(Func<List<string>, bool> mutate)
    {
        const int maxAttempts = 5;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            var (index, etag) = await daprClient.GetStateAndETagAsync<List<string>>(StateStoreName, EscalationQueueKey);
            index ??= [];

            if (!mutate(index))
            {
                return;
            }

            var saved = await daprClient.TrySaveStateAsync(StateStoreName, EscalationQueueKey, index, etag);
            if (saved)
            {
                return;
            }

            logger.LogWarning(
                "Escalation queue index update conflicted with a concurrent writer on attempt {Attempt}/{MaxAttempts}; retrying.",
                attempt,
                maxAttempts);
        }

        throw new InvalidOperationException(
            $"Failed to update escalation queue index after {maxAttempts} attempts due to concurrent writes.");
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
}
