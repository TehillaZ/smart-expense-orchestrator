namespace ApprovalFlow.Reliability.Outbox;

public interface IOutboxStore
{
    Task<OutboxMessage?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxMessage>> GetPendingMessagesAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default);
}
