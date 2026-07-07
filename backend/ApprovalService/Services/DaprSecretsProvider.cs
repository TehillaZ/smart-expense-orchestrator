using Dapr.Client;

namespace ApprovalService.Services;

public sealed class DaprSecretsProvider
{
    private const string SecretStoreName = "local-secret-store";

    private readonly DaprClient daprClient;
    private readonly ILogger<DaprSecretsProvider> logger;

    public DaprSecretsProvider(DaprClient daprClient, ILogger<DaprSecretsProvider> logger)
    {
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public async Task<string?> TryGetSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var secret = await daprClient.GetSecretAsync(SecretStoreName, key, cancellationToken: cancellationToken);
            return secret.Values.FirstOrDefault();
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to read secret {SecretKey} from Dapr secret store {SecretStoreName}.",
                key,
                SecretStoreName);
            return null;
        }
    }
}
