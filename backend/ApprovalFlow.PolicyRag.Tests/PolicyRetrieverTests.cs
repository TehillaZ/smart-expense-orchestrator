using ApprovalFlow.PolicyRag;

namespace ApprovalFlow.PolicyRag.Tests;

public class PolicyRetrieverTests
{
    [Fact]
    public async Task RetrieveAsync_ForMealsCategory_ReturnsRelevantPolicyClause()
    {
        var retriever = PolicyRagTestSupport.CreateRetriever();

        var result = await retriever.RetrieveAsync("meals", amount: 60m);

        Assert.Contains(result.Clauses, clause => clause.RuleId == "MEAL-01");
        Assert.Contains("MEAL-01", result.PromptContext, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RetrieveAsync_ForUnknownCategory_ReturnsFallbackGeneralRules()
    {
        var retriever = PolicyRagTestSupport.CreateRetriever();

        var result = await retriever.RetrieveAsync("unknown", amount: 120m);

        Assert.Contains(result.Clauses, clause => clause.RuleId.StartsWith("GLOBAL", StringComparison.Ordinal));
        Assert.Contains(result.Clauses, clause => clause.RuleId == "AUTONOMY-CEILING");
        Assert.Contains("GLOBAL", result.PromptContext, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RetrieveAsync_ForHardwareCategory_IncludesHardwareClause()
    {
        var retriever = PolicyRagTestSupport.CreateRetriever();

        var result = await retriever.RetrieveAsync("hardware", amount: 900m);

        Assert.Contains(result.Clauses, clause => clause.RuleId is "HW-01" or "HW-02");
    }
}
