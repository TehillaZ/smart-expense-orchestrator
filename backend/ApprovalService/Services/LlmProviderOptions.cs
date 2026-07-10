namespace ApprovalService.Services;

public sealed class LlmProviderOptions
{
    public const string SectionName = "LlmProvider";

    public string Provider { get; init; } = LlmProviderNames.Stub;

    public string Model { get; init; } = "gemini-2.0-flash";

    public string BaseUrl { get; init; } = "https://generativelanguage.googleapis.com/v1beta/";

    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Optional secondary provider (e.g. Groq) used after primary retries are exhausted.
    /// </summary>
    public string? FallbackProvider { get; init; }

    public string? FallbackModel { get; init; }

    public string? FallbackBaseUrl { get; init; }

    /// <summary>
    /// Extra attempts on the primary provider after the first failure (transient errors only).
    /// </summary>
    public int RetryCount { get; init; } = 1;

    public int RetryDelayMs { get; init; } = 500;
}

public static class LlmProviderNames
{
    public const string Stub = "Stub";
    public const string OpenRouter = "OpenRouter";
    public const string Gemini = "Gemini";
    public const string Groq = "Groq";

    public static bool RequiresApiKey(string? providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return false;
        }

        return string.Equals(providerName, OpenRouter, StringComparison.OrdinalIgnoreCase)
            || string.Equals(providerName, Gemini, StringComparison.OrdinalIgnoreCase)
            || string.Equals(providerName, Groq, StringComparison.OrdinalIgnoreCase);
    }

    public static string DefaultModel(string providerName) =>
        providerName.Trim().ToUpperInvariant() switch
        {
            "OPENROUTER" => "openai/gpt-4o-mini",
            "GEMINI" => "gemini-2.0-flash",
            "GROQ" => "llama-3.3-70b-versatile",
            _ => "stub"
        };

    public static string DefaultBaseUrl(string providerName) =>
        providerName.Trim().ToUpperInvariant() switch
        {
            "OPENROUTER" => "https://openrouter.ai/api/v1/",
            "GEMINI" => "https://generativelanguage.googleapis.com/v1beta/",
            "GROQ" => "https://api.groq.com/openai/v1/",
            _ => "http://localhost/"
        };
}
