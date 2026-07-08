namespace ApprovalFlow.Reliability.Outbox;

public interface IOutboxTransport
{
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
