using System.Text.RegularExpressions;

namespace ApprovalFlow.PolicyRag;

public static partial class PolicyDocumentChunker
{
    private static readonly IReadOnlyDictionary<string, string> SectionCategoryMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Meals & Entertainment"] = "meals",
            ["Travel"] = "travel",
            ["Software / SaaS"] = "saas",
            ["Hardware"] = "hardware",
        };

    public static IReadOnlyList<PolicyClause> Chunk(string policyMarkdown)
    {
        var clauses = new List<PolicyClause>();
        var currentSection = "unknown";
        var currentCategories = Array.Empty<string>();

        foreach (var rawLine in policyMarkdown.Split('\n'))
        {
            var line = rawLine.Trim();

            var sectionMatch = SectionHeaderRegex().Match(line);
            if (sectionMatch.Success)
            {
                currentSection = sectionMatch.Groups["title"].Value.Trim();
                currentCategories = ResolveCategories(currentSection);
                continue;
            }

            var ruleMatch = RuleRowRegex().Match(line);
            if (ruleMatch.Success)
            {
                var ruleId = ruleMatch.Groups["ruleId"].Value.Trim();
                var text = ruleMatch.Groups["text"].Value.Trim();
                clauses.Add(new PolicyClause(ruleId, text, currentSection, currentCategories));
                continue;
            }

            var autonomyMatch = AutonomyRowRegex().Match(line);
            if (autonomyMatch.Success)
            {
                var ruleId = autonomyMatch.Groups["ruleId"].Value.Trim();
                var text = $"{autonomyMatch.Groups["detail"].Value.Trim()} {autonomyMatch.Groups["meaning"].Value.Trim()}".Trim();
                clauses.Add(new PolicyClause(ruleId, text, currentSection, currentCategories));
            }
        }

        return clauses;
    }

    public static IReadOnlyList<PolicyClause> ChunkFromFile(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Policy document not found at '{path}'.", path);
        }

        return Chunk(File.ReadAllText(path));
    }

    private static string[] ResolveCategories(string sectionTitle)
    {
        if (string.Equals(sectionTitle, "Global rules", StringComparison.OrdinalIgnoreCase))
        {
            return ["global"];
        }

        if (string.Equals(sectionTitle, "Autonomy thresholds (the dilemma — **default posture; tune & justify**)", StringComparison.OrdinalIgnoreCase)
            || sectionTitle.StartsWith("Autonomy thresholds", StringComparison.OrdinalIgnoreCase))
        {
            return ["autonomy"];
        }

        foreach (var entry in SectionCategoryMap)
        {
            if (sectionTitle.Contains(entry.Key, StringComparison.OrdinalIgnoreCase))
            {
                return [entry.Value];
            }
        }

        return [];
    }

    [GeneratedRegex(@"^##\s+\d+\.\s+(?<title>.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex SectionHeaderRegex();

    [GeneratedRegex(@"^\|\s*`(?<ruleId>[A-Z0-9-]+)`\s*\|\s*(?<text>.+?)\s*\|$", RegexOptions.CultureInvariant)]
    private static partial Regex RuleRowRegex();

    [GeneratedRegex(@"^\|\s*`(?<ruleId>AUTONOMY-[A-Z-]+)`\s*\|\s*(?<detail>[^|]+)\|\s*(?<meaning>.+?)\s*\|$", RegexOptions.CultureInvariant)]
    private static partial Regex AutonomyRowRegex();
}
