namespace ApprovalFlow.PolicyRag;

public static class SimpleEmbeddingGenerator
{
    public static IReadOnlyDictionary<string, float> CreateEmbedding(string text)
    {
        var tokens = Tokenize(text).ToList();
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var token in tokens)
        {
            counts[token] = counts.TryGetValue(token, out var count) ? count + 1 : 1;
        }

        var total = tokens.Count;
        if (total == 0)
        {
            return new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        }

        return counts.ToDictionary(
            pair => pair.Key,
            pair => pair.Value / (float)total,
            StringComparer.OrdinalIgnoreCase);
    }

    public static float CosineSimilarity(
        IReadOnlyDictionary<string, float> left,
        IReadOnlyDictionary<string, float> right)
    {
        if (left.Count == 0 || right.Count == 0)
        {
            return 0f;
        }

        var dot = 0f;
        var leftMagnitude = 0f;
        var rightMagnitude = 0f;

        foreach (var (term, weight) in left)
        {
            leftMagnitude += weight * weight;
            if (right.TryGetValue(term, out var rightWeight))
            {
                dot += weight * rightWeight;
            }
        }

        foreach (var weight in right.Values)
        {
            rightMagnitude += weight * weight;
        }

        if (leftMagnitude == 0f || rightMagnitude == 0f)
        {
            return 0f;
        }

        return dot / (MathF.Sqrt(leftMagnitude) * MathF.Sqrt(rightMagnitude));
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        return text
            .Split([' ', '\t', '\r', '\n', ',', '.', ';', ':', '|', '`', '(', ')', '[', ']', '*', '$', '—', '-'], StringSplitOptions.RemoveEmptyEntries)
            .Where(token => token.Length > 2)
            .Select(token => token.ToLowerInvariant());
    }
}
