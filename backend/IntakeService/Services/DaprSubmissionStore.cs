using System.Net;
using Dapr.Client;
using IntakeService.Models;

namespace IntakeService.Services;

public sealed class DaprSubmissionStore : ISubmissionStore
{
    private const string StateStoreName = "approval-state";
    private const string InitialStatus = "Received";
    private const string InitialReason = "Invoice received and is pending analysis.";

    private readonly DaprClient daprClient;
    private readonly ILogger<DaprSubmissionStore> logger;

    public DaprSubmissionStore(DaprClient daprClient, ILogger<DaprSubmissionStore> logger)
    {
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public async Task<SubmissionStatusResponse> CreateSubmissionAsync(SubmissionRequest request, string correlationId)
    {
        var trackingId = Guid.NewGuid().ToString();

        var submission = new SubmissionStatusResponse(
            trackingId,
            correlationId,
            request.Vendor!.Trim(),
            request.InvoiceNumber!.Trim(),
            request.Amount,
            request.Category!.Trim(),
            InitialStatus,
            InitialReason);

        try
        {
            await daprClient.SaveStateAsync(StateStoreName, trackingId, submission);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to save submission {TrackingId} to the {StateStoreName} state store.",
                trackingId,
                StateStoreName);
            throw;
        }

        return submission;
    }

    public async Task<SubmissionStatusResponse?> GetSubmissionAsync(string trackingId)
    {
        try
        {
            var workflowStatus = await TryGetWorkflowStatusFromApprovalServiceAsync(trackingId);
            if (workflowStatus is not null)
            {
                return workflowStatus;
            }

            return await daprClient.GetStateAsync<SubmissionStatusResponse>(StateStoreName, trackingId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to read submission {TrackingId} from the {StateStoreName} state store.",
                trackingId,
                StateStoreName);
            throw;
        }
    }

    private async Task<SubmissionStatusResponse?> TryGetWorkflowStatusFromApprovalServiceAsync(string trackingId)
    {
        try
        {
            return await daprClient.InvokeMethodAsync<SubmissionStatusResponse>(
                HttpMethod.Get,
                DaprServiceIds.ApprovalService,
                $"submissions/{trackingId}");
        }
        catch (InvocationException ex) when (ex.Response?.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogDebug(
                "ApprovalService has no workflow state yet for submission {TrackingId}. Falling back to intake state.",
                trackingId);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to read workflow status for submission {TrackingId} from ApprovalService. Falling back to intake state.",
                trackingId);
            return null;
        }
    }
}
