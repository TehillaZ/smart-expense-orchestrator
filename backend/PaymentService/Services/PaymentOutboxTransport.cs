using System.Text.Json;
using ApprovalFlow.Reliability.Outbox;
using PaymentService.Models;

namespace PaymentService.Services;

public sealed class PaymentOutboxTransport : IOutboxTransport
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly Dapr.Client.DaprClient daprClient;
    private readonly ILogger<PaymentOutboxTransport> logger;

    public PaymentOutboxTransport(Dapr.Client.DaprClient daprClient, ILogger<PaymentOutboxTransport> logger)
    {
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        if (string.Equals(message.Topic, PaymentOutboxTopics.PaymentCompleted, StringComparison.Ordinal))
        {
            var completed = JsonSerializer.Deserialize<PaymentCompletedEvent>(message.PayloadJson, SerializerOptions)
                ?? throw new InvalidOperationException($"Unable to deserialize outbox payload for {message.IdempotencyKey}.");
            await daprClient.PublishEventAsync(message.PubSubName, message.Topic, completed, cancellationToken);
        }
        else if (string.Equals(message.Topic, PaymentOutboxTopics.PaymentFailed, StringComparison.Ordinal))
        {
            var failed = JsonSerializer.Deserialize<PaymentFailedEvent>(message.PayloadJson, SerializerOptions)
                ?? throw new InvalidOperationException($"Unable to deserialize outbox payload for {message.IdempotencyKey}.");
            await daprClient.PublishEventAsync(message.PubSubName, message.Topic, failed, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported outbox topic '{message.Topic}'.");
        }

        logger.LogInformation(
            "Outbox transport published {Topic} for {IdempotencyKey}.",
            message.Topic,
            message.IdempotencyKey);
    }
}

public static class PaymentOutboxTopics
{
    public const string PaymentCompleted = "payment.completed";
    public const string PaymentFailed = "payment.failed";
}
