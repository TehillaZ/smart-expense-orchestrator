namespace NotificationService.Services;

public sealed class DaprSecretsBootstrapper : IHostedService
{
    private readonly DaprSecretsProvider secretsProvider;
    private readonly NotificationChannelOptions channelOptions;
    private readonly ILogger<DaprSecretsBootstrapper> logger;

    public DaprSecretsBootstrapper(
        DaprSecretsProvider secretsProvider,
        NotificationChannelOptions channelOptions,
        ILogger<DaprSecretsBootstrapper> logger)
    {
        this.secretsProvider = secretsProvider;
        this.channelOptions = channelOptions;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        channelOptions.SenderAddress =
            await secretsProvider.TryGetSecretAsync("Notification:SenderAddress", cancellationToken);

        if (string.IsNullOrWhiteSpace(channelOptions.SenderAddress))
        {
            logger.LogWarning(
                "Notification sender address was not loaded from Dapr; stub channel will omit sender metadata.");
            return;
        }

        logger.LogInformation(
            "Notification sender address loaded from Dapr secret store for stub delivery logging.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
