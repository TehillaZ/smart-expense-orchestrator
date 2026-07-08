using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApprovalFlow.PolicyRag;

public sealed class PolicyRetriever : IPolicyRetriever
{
    private static readonly string[] FallbackCategories = ["global", "autonomy"];
    private static readonly string[] AutonomyRuleIds = ["AUTONOMY-CEILING", "AUTONOMY-CONFIDENCE", "AUTONOMY-HARDSTOPS"];

    private readonly InMemoryPolicyVectorStore vectorStore;
    private readonly PolicyRagOptions options;
    private readonly ILogger<PolicyRetriever> logger;

    public PolicyRetriever(
        InMemoryPolicyVectorStore vectorStore,
        IOptions<PolicyRagOptions> options,
        ILogger<PolicyRetriever> logger)
    {
        this.vectorStore = vectorStore;
        this.options = options.Value;
        this.logger = logger;
    }

    public Task<PolicyRetrievalResult> RetrieveAsync(
        string category,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        var normalizedCategory = category.Trim();
        var isKnownCategory = IsKnownCategory(normalizedCategory);
        var preferredCategories = isKnownCategory
            ? [normalizedCategory, "autonomy"]
            : FallbackCategories;

        var query = BuildQuery(normalizedCategory, amount, isKnownCategory);
        var retrieved = vectorStore.Search(query, preferredCategories, options.MaxRetrievedClauses);

        if (!isKnownCategory)
        {
            retrieved = MergeDistinct(
                retrieved,
                vectorStore.GetByRuleIds(["GLOBAL-VENDOR", "GLOBAL-RECEIPT"]),
                vectorStore.GetByRuleIds(AutonomyRuleIds));
        }
        else
        {
            retrieved = MergeDistinct(
                retrieved,
                vectorStore.GetByRuleIds(AutonomyRuleIds));
        }

        retrieved = retrieved
            .Take(options.MaxRetrievedClauses)
            .ToList();

        var promptContext = string.Join(
            " ",
            retrieved.Select(clause => $"[{clause.RuleId}] {clause.Text}"));

        logger.LogInformation(
            "Retrieved {ClauseCount} policy clauses for category {Category} and amount {Amount}.",
            retrieved.Count,
            normalizedCategory,
            amount);

        return Task.FromResult(new PolicyRetrievalResult(retrieved, promptContext));
    }

    private static string BuildQuery(string category, decimal amount, bool isKnownCategory)
    {
        if (isKnownCategory)
        {
            return $"{category} expense amount {amount:0.##} reimbursement approval";
        }

        return $"unknown category {category} amount {amount:0.##} general fallback policy autonomy";
    }

    private static bool IsKnownCategory(string category)
    {
        return category is "meals" or "travel" or "saas" or "hardware";
    }

    private static IReadOnlyList<PolicyClause> MergeDistinct(
        params IReadOnlyList<PolicyClause>[] groups)
    {
        var merged = new List<PolicyClause>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            foreach (var clause in group)
            {
                if (seen.Add(clause.RuleId))
                {
                    merged.Add(clause);
                }
            }
        }

        return merged;
    }
}
