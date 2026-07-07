namespace ApprovalService.Services;

public sealed class DaprSecretsBootstrapper : IHostedService
{
    private readonly DaprSecretsProvider secretsProvider;
    private readonly ILogger<DaprSecretsBootstrapper> logger;

    public DaprSecretsBootstrapper(
        DaprSecretsProvider secretsProvider,
        ILogger<DaprSecretsBootstrapper> logger)
    {
        this.secretsProvider = secretsProvider;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var apiKey = await secretsProvider.TryGetSecretAsync("LlmProvider:ApiKey", cancellationToken);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning(
                "LLM provider secret was not loaded from Dapr; continuing with the configured stub provider.");
            return;
        }

        logger.LogInformation(
            "LLM provider configuration loaded from Dapr secret store. External provider calls remain disabled in this demo build.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
