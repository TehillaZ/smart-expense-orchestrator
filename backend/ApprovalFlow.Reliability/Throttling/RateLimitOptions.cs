namespace ApprovalFlow.Reliability.Throttling;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public int PermitLimit { get; set; } = 5;
    public int WindowSeconds { get; set; } = 1;
}
