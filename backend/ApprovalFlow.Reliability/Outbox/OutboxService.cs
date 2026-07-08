using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApprovalFlow.Reliability.Outbox;

public interface IOutboxService
{
    Task EnqueueAsync<TPayload>(
        string idempotencyKey,
        string pubSubName,
        string topic,
        TPayload payload,
        CancellationToken cancellationToken = default);
}

public sealed class OutboxService : IOutboxService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IOutboxStore store;
    private readonly IOutboxTransport transport;
    private readonly OutboxOptions options;
    private readonly ILogger<OutboxService> logger;

    public OutboxService(
        IOutboxStore store,
        IOutboxTransport transport,
        IOptions<OutboxOptions> options,
        ILogger<OutboxService> logger)
    {
        this.store = store;
        this.transport = transport;
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task EnqueueAsync<TPayload>(
        string idempotencyKey,
        string pubSubName,
        string topic,
        TPayload payload,
        CancellationToken cancellationToken = default)
    {
        var existing = await store.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existing is not null
            && string.Equals(existing.Status, OutboxMessageStatus.Published, StringComparison.Ordinal))
        {
            logger.LogInformation(
                "Outbox message {IdempotencyKey} is already published. Skipping duplicate enqueue.",
                idempotencyKey);
            return;
        }

        var message = existing ?? CreateMessage(idempotencyKey, pubSubName, topic, payload);
        if (existing is null)
        {
            await store.SaveAsync(message, cancellationToken);
        }

        await TryDispatchAsync(message, cancellationToken);
    }

    public async Task<bool> TryDispatchAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (string.Equals(message.Status, OutboxMessageStatus.Published, StringComparison.Ordinal))
        {
            return true;
        }

        try
        {
            await transport.PublishAsync(message, cancellationToken);

            var published = message with
            {
                Status = OutboxMessageStatus.Published,
                NextAttemptAt = DateTimeOffset.UtcNow
            };
            await store.SaveAsync(published, cancellationToken);

            logger.LogInformation(
                "Published outbox message {IdempotencyKey} to {Topic}.",
                message.IdempotencyKey,
                message.Topic);
            return true;
        }
        catch (Exception ex)
        {
            var nextAttemptCount = message.AttemptCount + 1;
            var status = nextAttemptCount >= options.MaxAttempts
                ? OutboxMessageStatus.Failed
                : OutboxMessageStatus.Pending;

            var failed = message with
            {
                Status = status,
                AttemptCount = nextAttemptCount,
                NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(options.RetryDelaySeconds)
            };
            await store.SaveAsync(failed, cancellationToken);

            logger.LogError(
                ex,
                "Failed to publish outbox message {IdempotencyKey} on attempt {AttemptCount}.",
                message.IdempotencyKey,
                nextAttemptCount);
            return false;
        }
    }

    private OutboxMessage CreateMessage<TPayload>(
        string idempotencyKey,
        string pubSubName,
        string topic,
        TPayload payload)
    {
        var payloadType = typeof(TPayload).FullName ?? typeof(TPayload).Name;
        var payloadJson = JsonSerializer.Serialize(payload, SerializerOptions);
        var now = DateTimeOffset.UtcNow;

        return new OutboxMessage(
            idempotencyKey,
            pubSubName,
            topic,
            payloadType,
            payloadJson,
            OutboxMessageStatus.Pending,
            AttemptCount: 0,
            CreatedAt: now,
            NextAttemptAt: now);
    }
}
