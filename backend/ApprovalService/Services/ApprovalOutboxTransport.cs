using System.Text.Json;
using ApprovalFlow.Reliability.Outbox;
using Dapr.Client;

namespace ApprovalService.Services;

public sealed class ApprovalOutboxTransport : IOutboxTransport
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly DaprClient daprClient;
    private readonly ILogger<ApprovalOutboxTransport> logger;

    public ApprovalOutboxTransport(DaprClient daprClient, ILogger<ApprovalOutboxTransport> logger)
    {
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<PaymentRequestedEvent>(message.PayloadJson, SerializerOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize outbox payload for {message.IdempotencyKey}.");

        await daprClient.PublishEventAsync(
            message.PubSubName,
            message.Topic,
            payload,
            cancellationToken);

        logger.LogInformation(
            "Outbox transport published {Topic} for {IdempotencyKey}.",
            message.Topic,
            message.IdempotencyKey);
    }
}
