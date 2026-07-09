using ApprovalFlow.PolicyRag;

namespace ApprovalFlow.PolicyRag.Tests;

public class PolicyDocumentChunkerTests
{
    [Fact]
    public void Chunk_ParsesRuleIdsFromPolicyTables()
    {
        var clauses = PolicyDocumentChunker.ChunkFromFile(PolicyDocumentLocator.Resolve("Data/policy.md"));

        Assert.Contains(clauses, clause => clause.RuleId == "MEAL-01");
        Assert.Contains(clauses, clause => clause.RuleId == "HW-02");
        Assert.Contains(clauses, clause => clause.RuleId == "AUTONOMY-CEILING");
    }
}
