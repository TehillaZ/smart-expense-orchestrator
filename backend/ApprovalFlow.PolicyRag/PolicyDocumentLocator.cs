namespace ApprovalFlow.PolicyRag;

public static class PolicyDocumentLocator
{
    public static string Resolve(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath) && File.Exists(configuredPath))
        {
            return configuredPath;
        }

        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), configuredPath),
            Path.Combine(AppContext.BaseDirectory, configuredPath),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "ApprovalService", configuredPath)),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ApprovalService", configuredPath)),
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(
            $"Policy document could not be resolved from configured path '{configuredPath}'.",
            configuredPath);
    }
}
