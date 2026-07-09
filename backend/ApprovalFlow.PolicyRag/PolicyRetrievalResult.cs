namespace ApprovalFlow.PolicyRag;

public sealed record PolicyRetrievalResult(
    IReadOnlyList<PolicyClause> Clauses,
    string PromptContext);
