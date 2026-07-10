using Microsoft.Extensions.Options;

namespace ApprovalService.Services;

public sealed class DaprSecretsBootstrapper : IHostedService
{
    private const int MaxAttempts = 30;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    private readonly DaprSecretsProvider secretsProvider;
    private readonly LlmApiKeyAccessor apiKeyAccessor;
    private readonly LlmProviderOptions options;
    private readonly ILogger<DaprSecretsBootstrapper> logger;
    private CancellationTokenSource? backgroundCts;
    private Task? backgroundTask;

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

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Dapr sidecar often starts after the app becomes healthy, so load secrets in the background.
        backgroundCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        backgroundTask = LoadSecretWithRetriesAsync(backgroundCts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (backgroundCts is null)
        {
            return;
        }

        await backgroundCts.CancelAsync();
        if (backgroundTask is not null)
        {
            try
            {
                await backgroundTask.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
        }

        backgroundCts.Dispose();
    }

    private async Task LoadSecretWithRetriesAsync(CancellationToken cancellationToken)
    {
        var needsFallbackKey = LlmProviderNames.RequiresApiKey(options.FallbackProvider);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var apiKey = await secretsProvider.TryGetSecretAsync("LlmProvider:ApiKey", cancellationToken);
            var fallbackApiKey = await secretsProvider.TryGetSecretAsync(
                "LlmProvider:FallbackApiKey",
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                apiKeyAccessor.SetApiKey(apiKey);
                if (!string.IsNullOrWhiteSpace(fallbackApiKey))
                {
                    apiKeyAccessor.SetFallbackApiKey(fallbackApiKey);
                }

                logger.LogInformation(
                    "LLM provider API key loaded from Dapr secret store for provider {Provider}.",
                    options.Provider);

                if (needsFallbackKey && string.IsNullOrWhiteSpace(fallbackApiKey))
                {
                    logger.LogWarning(
                        "Fallback provider {FallbackProvider} is configured but LlmProvider:FallbackApiKey was not found.",
                        options.FallbackProvider);
                }
                else if (!string.IsNullOrWhiteSpace(fallbackApiKey))
                {
                    logger.LogInformation(
                        "LLM fallback API key loaded from Dapr secret store for provider {FallbackProvider}.",
                        options.FallbackProvider ?? "(configured)");
                }

                return;
            }

            logger.LogInformation(
                "Waiting for Dapr secret store before loading LlmProvider:ApiKey (attempt {Attempt}/{MaxAttempts}).",
                attempt,
                MaxAttempts);
            await Task.Delay(RetryDelay, cancellationToken);
        }

        if (LlmProviderNames.RequiresApiKey(options.Provider))
        {
            logger.LogError(
                "{Provider} is configured but LlmProvider:ApiKey was not loaded from Dapr. " +
                "Provider calls will fail cleanly until the secret is available.",
                options.Provider);
        }
        else
        {
            logger.LogWarning(
                "LLM provider secret was not loaded from Dapr; continuing with the configured {Provider} provider.",
                options.Provider);
        }
    }
}
