namespace ApprovalService.Services;

public sealed class LlmProviderOptions
{
    public const string SectionName = "LlmProvider";

    public string Provider { get; init; } = LlmProviderNames.Stub;
}

public static class LlmProviderNames
{
    public const string Stub = "Stub";
}
