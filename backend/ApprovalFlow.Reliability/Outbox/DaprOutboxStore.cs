using Dapr.Client;
using Microsoft.Extensions.Options;

namespace ApprovalFlow.Reliability.Outbox;

public sealed class DaprOutboxStore : IOutboxStore
{
    private readonly DaprClient daprClient;
    private readonly OutboxOptions options;

    public DaprOutboxStore(DaprClient daprClient, IOptions<OutboxOptions> options)
    {
        this.daprClient = daprClient;
        this.options = options.Value;
    }

    public async Task<OutboxMessage?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return await daprClient.GetStateAsync<OutboxMessage?>(
            options.StateStoreName,
            BuildMessageKey(idempotencyKey),
            cancellationToken: cancellationToken);
    }

    public async Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await daprClient.SaveStateAsync(
            options.StateStoreName,
            BuildMessageKey(message.IdempotencyKey),
            message,
            cancellationToken: cancellationToken);

        if (string.Equals(message.Status, OutboxMessageStatus.Pending, StringComparison.Ordinal))
        {
            await AddPendingKeyAsync(message.IdempotencyKey, cancellationToken);
        }
        else
        {
            await RemovePendingKeyAsync(message.IdempotencyKey, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingMessagesAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default)
    {
        var pendingKeys = await daprClient.GetStateAsync<List<string>>(
            options.StateStoreName,
            BuildPendingIndexKey(),
            cancellationToken: cancellationToken) ?? [];

        var messages = new List<OutboxMessage>();

        foreach (var idempotencyKey in pendingKeys)
        {
            var message = await GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (message is null)
            {
                continue;
            }

            if (string.Equals(message.Status, OutboxMessageStatus.Pending, StringComparison.Ordinal)
                && message.NextAttemptAt <= asOf)
            {
                messages.Add(message);
            }
        }

        return messages;
    }

    private async Task AddPendingKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        var pendingKeys = await daprClient.GetStateAsync<List<string>>(
            options.StateStoreName,
            BuildPendingIndexKey(),
            cancellationToken: cancellationToken) ?? [];

        if (!pendingKeys.Contains(idempotencyKey, StringComparer.Ordinal))
        {
            pendingKeys.Add(idempotencyKey);
            await daprClient.SaveStateAsync(
                options.StateStoreName,
                BuildPendingIndexKey(),
                pendingKeys,
                cancellationToken: cancellationToken);
        }
    }

    private async Task RemovePendingKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
    {
        var pendingKeys = await daprClient.GetStateAsync<List<string>>(
            options.StateStoreName,
            BuildPendingIndexKey(),
            cancellationToken: cancellationToken) ?? [];

        if (pendingKeys.RemoveAll(key => string.Equals(key, idempotencyKey, StringComparison.Ordinal)) > 0)
        {
            await daprClient.SaveStateAsync(
                options.StateStoreName,
                BuildPendingIndexKey(),
                pendingKeys,
                cancellationToken: cancellationToken);
        }
    }

    private string BuildMessageKey(string idempotencyKey) => $"outbox:{options.Scope}:{idempotencyKey}";

    private string BuildPendingIndexKey() => $"outbox:{options.Scope}:pending";
}
