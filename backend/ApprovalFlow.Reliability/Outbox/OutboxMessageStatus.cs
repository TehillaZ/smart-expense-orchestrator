namespace ApprovalFlow.Reliability.Outbox;

public static class OutboxMessageStatus
{
    public const string Pending = "Pending";
    public const string Published = "Published";
    public const string Failed = "Failed";
}
