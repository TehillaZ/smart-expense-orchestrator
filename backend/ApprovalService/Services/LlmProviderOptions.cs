namespace ApprovalService.Services;

public sealed class LlmProviderOptions
{
    public const string SectionName = "LlmProvider";

    public string Provider { get; init; } = LlmProviderNames.Stub;

    public string Model { get; init; } = "openai/gpt-4o-mini";

    public string BaseUrl { get; init; } = "https://openrouter.ai/api/v1";

    public int TimeoutSeconds { get; init; } = 30;
}

public static class LlmProviderNames
{
    public const string Stub = "Stub";
    public const string OpenRouter = "OpenRouter";
}
