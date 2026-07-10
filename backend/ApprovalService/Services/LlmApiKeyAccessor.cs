namespace ApprovalService.Services;

public interface ILlmApiKeyAccessor
{
    string? ApiKey { get; }
}

/// <summary>
/// Holds the LLM API keys loaded from Dapr secrets (or configuration) at startup.
/// </summary>
public sealed class LlmApiKeyAccessor : ILlmApiKeyAccessor
{
    private string? apiKey;
    private string? fallbackApiKey;

    public string? ApiKey => apiKey;

    public string? FallbackApiKey => fallbackApiKey;

    public void SetApiKey(string? value)
    {
        apiKey = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public void SetFallbackApiKey(string? value)
    {
        fallbackApiKey = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public ILlmApiKeyAccessor PrimaryView => this;

    public ILlmApiKeyAccessor FallbackView => new FallbackKeyView(this);

    private sealed class FallbackKeyView : ILlmApiKeyAccessor
    {
        private readonly LlmApiKeyAccessor owner;

        public FallbackKeyView(LlmApiKeyAccessor owner)
        {
            this.owner = owner;
        }

        public string? ApiKey => owner.FallbackApiKey;
    }
}
