namespace ApprovalFlow.PolicyRag;

public sealed class InMemoryPolicyVectorStore
{
    private readonly IReadOnlyList<IndexedClause> indexedClauses;

    public InMemoryPolicyVectorStore(IReadOnlyList<PolicyClause> clauses)
    {
        indexedClauses = clauses
            .Select(clause => new IndexedClause(
                clause,
                SimpleEmbeddingGenerator.CreateEmbedding($"{clause.RuleId} {clause.Text} {string.Join(' ', clause.Categories)}")))
            .ToList();
    }

    public IReadOnlyList<PolicyClause> Search(
        string query,
        IReadOnlyList<string> preferredCategories,
        int maxResults)
    {
        var queryEmbedding = SimpleEmbeddingGenerator.CreateEmbedding(query);
        var ranked = indexedClauses
            .Select(indexed => new RankedClause(
                indexed.Clause,
                Score(indexed, queryEmbedding, preferredCategories)))
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Clause.RuleId, StringComparer.Ordinal)
            .Take(maxResults)
            .Select(item => item.Clause)
            .ToList();

        return ranked;
    }

    public IReadOnlyList<PolicyClause> GetByRuleIds(IEnumerable<string> ruleIds)
    {
        var lookup = ruleIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return indexedClauses
            .Where(indexed => lookup.Contains(indexed.Clause.RuleId))
            .Select(indexed => indexed.Clause)
            .ToList();
    }

    public IReadOnlyList<PolicyClause> GetByCategories(IEnumerable<string> categories)
    {
        var categorySet = categories.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return indexedClauses
            .Where(indexed => indexed.Clause.Categories.Any(category => categorySet.Contains(category)))
            .Select(indexed => indexed.Clause)
            .ToList();
    }

    private static float Score(
        IndexedClause indexed,
        IReadOnlyDictionary<string, float> queryEmbedding,
        IReadOnlyList<string> preferredCategories)
    {
        var semanticScore = SimpleEmbeddingGenerator.CosineSimilarity(queryEmbedding, indexed.Embedding);
        var categoryBoost = indexed.Clause.Categories.Any(category => preferredCategories.Contains(category, StringComparer.OrdinalIgnoreCase))
            ? 1.5f
            : 0f;

        return semanticScore + categoryBoost;
    }

    private sealed record IndexedClause(PolicyClause Clause, IReadOnlyDictionary<string, float> Embedding);

    private sealed record RankedClause(PolicyClause Clause, float Score);
}
