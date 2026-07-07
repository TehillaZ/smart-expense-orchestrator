using Dapr.Client;

namespace IntakeService.Services;

public sealed record PolicySettingsSnapshot(
    decimal AutonomyCeiling,
    decimal AutonomyConfidence,
    string PolicyVersion);

public sealed class DaprPolicyLookup
{
    private readonly DaprClient daprClient;
    private readonly ILogger<DaprPolicyLookup> logger;

    public DaprPolicyLookup(DaprClient daprClient, ILogger<DaprPolicyLookup> logger)
    {
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public async Task<PolicySettingsSnapshot> GetAsync()
    {
        try
        {
            var settings = await daprClient.InvokeMethodAsync<PolicySettingsSnapshot>(
                HttpMethod.Get,
                DaprServiceIds.ApprovalService,
                "policy");

            return settings;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to read policy settings from ApprovalService. Falling back to default policy snapshot.");
            return new PolicySettingsSnapshot(400m, 0.90m, "v1");
        }
    }
}
