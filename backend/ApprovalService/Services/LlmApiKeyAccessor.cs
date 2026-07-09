namespace ApprovalService.Services;

/// <summary>
/// Holds the LLM API key loaded from Dapr secrets (or configuration) at startup.
/// </summary>
public sealed class LlmApiKeyAccessor
{
    private string? apiKey;

    public string? ApiKey => apiKey;

    public void SetApiKey(string? value)
    {
        apiKey = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
