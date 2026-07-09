using Microsoft.Extensions.Options;

namespace ApprovalService.Services;

public sealed class DaprSecretsBootstrapper : IHostedService
{
    private readonly DaprSecretsProvider secretsProvider;
    private readonly LlmApiKeyAccessor apiKeyAccessor;
    private readonly LlmProviderOptions options;
    private readonly ILogger<DaprSecretsBootstrapper> logger;

    public DaprSecretsBootstrapper(
        DaprSecretsProvider secretsProvider,
        LlmApiKeyAccessor apiKeyAccessor,
        IOptions<LlmProviderOptions> options,
        ILogger<DaprSecretsBootstrapper> logger)
    {
        this.secretsProvider = secretsProvider;
        this.apiKeyAccessor = apiKeyAccessor;
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var apiKey = await secretsProvider.TryGetSecretAsync("LlmProvider:ApiKey", cancellationToken);
        apiKeyAccessor.SetApiKey(apiKey);

        if (string.IsNullOrWhiteSpace(apiKeyAccessor.ApiKey))
        {
            if (string.Equals(options.Provider, LlmProviderNames.OpenRouter, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError(
                    "OpenRouter is configured but LlmProvider:ApiKey was not loaded from Dapr. " +
                    "Provider calls will fail cleanly until the secret is available.");
            }
            else
            {
                logger.LogWarning(
                    "LLM provider secret was not loaded from Dapr; continuing with the configured {Provider} provider.",
                    options.Provider);
            }

            return;
        }

        logger.LogInformation(
            "LLM provider API key loaded from Dapr secret store for provider {Provider}.",
            options.Provider);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
