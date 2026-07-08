namespace ApprovalFlow.PolicyRag;

public sealed record PolicyClause(
    string RuleId,
    string Text,
    string Section,
    IReadOnlyList<string> Categories);
