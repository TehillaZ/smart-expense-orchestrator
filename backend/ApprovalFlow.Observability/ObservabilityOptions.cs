namespace ApprovalFlow.Observability;

public sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public bool Enabled { get; set; } = true;

    public string OtlpEndpoint { get; set; } = "http://localhost:4317";
}
