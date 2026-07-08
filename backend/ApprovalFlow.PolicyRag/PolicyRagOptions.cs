namespace ApprovalFlow.PolicyRag;

public sealed class PolicyRagOptions
{
    public const string SectionName = "PolicyRag";

    public string PolicyDocumentPath { get; set; } = "Data/policy.md";

    public int MaxRetrievedClauses { get; set; } = 5;
}
