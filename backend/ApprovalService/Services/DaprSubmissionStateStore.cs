using ApprovalService.Models;
using Dapr.Client;

namespace ApprovalService.Services;

public sealed class DaprSubmissionStateStore : ISubmissionStateStore
{
    private const string StateStoreName = "approval-state";

    private readonly DaprClient daprClient;

    public DaprSubmissionStateStore(DaprClient daprClient)
    {
        this.daprClient = daprClient;
    }

    public async Task<SubmissionState?> GetAsync(string trackingId)
    {
        return await daprClient.GetStateAsync<SubmissionState>(StateStoreName, trackingId);
    }

    public Task SaveAsync(SubmissionState state)
    {
        return daprClient.SaveStateAsync(StateStoreName, state.TrackingId, state);
    }
}
