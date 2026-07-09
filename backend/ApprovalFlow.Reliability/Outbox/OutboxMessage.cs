namespace ApprovalFlow.Reliability.Outbox;

public sealed record OutboxMessage(
    string IdempotencyKey,
    string PubSubName,
    string Topic,
    string PayloadType,
    string PayloadJson,
    string Status,
    int AttemptCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset NextAttemptAt,
    string? CorrelationId = null);
