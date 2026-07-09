using ApprovalFlow.Reliability.Outbox;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ApprovalFlow.Reliability.Tests;

public sealed class InMemoryOutboxStore : IOutboxStore
{
    private readonly Dictionary<string, OutboxMessage> messages = new(StringComparer.Ordinal);

    public Task<OutboxMessage?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        messages.TryGetValue(idempotencyKey, out var message);
        return Task.FromResult(message);
    }

    public Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        messages[message.IdempotencyKey] = message;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OutboxMessage>> GetPendingMessagesAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OutboxMessage> pending = messages.Values
            .Where(message => string.Equals(message.Status, OutboxMessageStatus.Pending, StringComparison.Ordinal)
                && message.NextAttemptAt <= asOf)
            .ToList();
        return Task.FromResult(pending);
    }
}

public sealed class FlakyOutboxTransport : IOutboxTransport
{
    private int attempts;

    public int SuccessfulAttempts { get; private set; }

    public Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        attempts++;
        if (attempts == 1)
        {
            throw new InvalidOperationException("Simulated publish failure.");
        }

        SuccessfulAttempts++;
        return Task.CompletedTask;
    }
}

public sealed class OutboxServiceTests
{
    [Fact]
    public async Task EnqueueAsync_RetriesAfterPublishFailure()
    {
        var store = new InMemoryOutboxStore();
        var transport = new FlakyOutboxTransport();
        var service = CreateService(store, transport);

        await service.EnqueueAsync(
            "submission.received:tracking-1",
            "order-pubsub",
            "submission.received",
            new SamplePayload("tracking-1"));

        var pending = await store.GetByIdempotencyKeyAsync("submission.received:tracking-1");
        Assert.NotNull(pending);
        Assert.Equal(OutboxMessageStatus.Pending, pending!.Status);
        Assert.Equal(1, pending.AttemptCount);

        var retried = await service.TryDispatchAsync(pending!, CancellationToken.None);
        Assert.True(retried);

        var published = await store.GetByIdempotencyKeyAsync("submission.received:tracking-1");
        Assert.NotNull(published);
        Assert.Equal(OutboxMessageStatus.Published, published!.Status);
        Assert.Equal(1, transport.SuccessfulAttempts);
    }

    [Fact]
    public async Task EnqueueAsync_DuplicateRemainsIdempotent()
    {
        var store = new InMemoryOutboxStore();
        var transport = new CountingOutboxTransport();
        var service = CreateService(store, transport);

        await service.EnqueueAsync(
            "payment.requested:tracking-1",
            "order-pubsub",
            "payment.requested",
            new SamplePayload("tracking-1"));
        await service.EnqueueAsync(
            "payment.requested:tracking-1",
            "order-pubsub",
            "payment.requested",
            new SamplePayload("tracking-1"));

        Assert.Equal(1, transport.PublishCount);
    }

    [Fact]
    public async Task EnqueueAsync_CapturesCorrelationIdFromPayload()
    {
        var store = new InMemoryOutboxStore();
        var transport = new CountingOutboxTransport();
        var service = CreateService(store, transport);

        await service.EnqueueAsync(
            "submission.received:tracking-corr",
            "order-pubsub",
            "submission.received",
            new CorrelatedPayload("tracking-corr", "corr-42"));

        var message = await store.GetByIdempotencyKeyAsync("submission.received:tracking-corr");
        Assert.NotNull(message);
        Assert.Equal("corr-42", message!.CorrelationId);
    }

    [Theory]
    [InlineData("{\"correlationId\":\"abc-1\"}", "abc-1")]
    [InlineData("{\"CorrelationId\":\"abc-2\"}", "abc-2")]
    [InlineData("{\"trackingId\":\"only\"}", null)]
    public void TryExtractCorrelationId_ReadsCamelOrPascalCase(string payloadJson, string? expected)
    {
        var actual = OutboxService.TryExtractCorrelationId(payloadJson);
        Assert.Equal(expected, actual);
    }

    private static OutboxService CreateService(IOutboxStore store, IOutboxTransport transport)
    {
        return new OutboxService(
            store,
            transport,
            Options.Create(new OutboxOptions
            {
                Scope = "tests",
                RetryDelaySeconds = 1,
                MaxAttempts = 5
            }),
            NullLogger<OutboxService>.Instance);
    }

    private sealed record SamplePayload(string TrackingId);

    private sealed record CorrelatedPayload(string TrackingId, string CorrelationId);
}

public sealed class CountingOutboxTransport : IOutboxTransport
{
    public int PublishCount { get; private set; }

    public Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        PublishCount++;
        return Task.CompletedTask;
    }
}
