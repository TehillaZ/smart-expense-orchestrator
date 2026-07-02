using ApprovalService.Models;
using Dapr.Client;

namespace ApprovalService.Services;

public sealed class DaprPolicySettingsStore : IPolicySettingsStore
{
    private const string StateStoreName = "approval-state";
    private const string SettingsKey = "policy-settings";

    private readonly DaprClient daprClient;

    public DaprPolicySettingsStore(DaprClient daprClient)
    {
        this.daprClient = daprClient;
    }

    public async Task<PolicySettings> GetAsync()
    {
        var settings = await daprClient.GetStateAsync<PolicySettings>(StateStoreName, SettingsKey);
        return settings ?? PolicySettings.Default;
    }

    public Task SaveAsync(PolicySettings settings)
    {
        return daprClient.SaveStateAsync(StateStoreName, SettingsKey, settings);
    }
}
