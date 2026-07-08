namespace ApprovalFlow.Reliability.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public string Scope { get; set; } = "default";
    public string StateStoreName { get; set; } = "approval-state";
    public int PollIntervalSeconds { get; set; } = 5;
    public int MaxAttempts { get; set; } = 10;
    public int RetryDelaySeconds { get; set; } = 15;
}
