using System.Text.Json;
using ApprovalFlow.Reliability.Outbox;
using IntakeService.Models;
using Dapr.Client;

namespace IntakeService.Services;

public sealed class IntakeOutboxTransport : IOutboxTransport
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly DaprClient daprClient;
    private readonly ILogger<IntakeOutboxTransport> logger;

    public IntakeOutboxTransport(DaprClient daprClient, ILogger<IntakeOutboxTransport> logger)
    {
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<SubmissionReceivedEvent>(message.PayloadJson, SerializerOptions)
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
