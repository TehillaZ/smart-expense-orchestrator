namespace ApprovalFlow.Reliability.Throttling;

public sealed class BulkheadOptions
{
    public const string SectionName = "Bulkhead";

    public int MaxConcurrency { get; set; } = 2;
}
