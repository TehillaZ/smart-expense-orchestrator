using System.Net;
using Dapr.Client;
using IntakeService.Models;

namespace IntakeService.Services;

public sealed class DaprSubmissionStore : ISubmissionStore
{
    private const string StateStoreName = "approval-state";
    private const string IndexKeyPrefix = "submission-index";
    private const string InitialStatus = "Received";
    private const string InitialReason = "Invoice received and is pending analysis.";

    private readonly DaprClient daprClient;
    private readonly ILogger<DaprSubmissionStore> logger;

    public DaprSubmissionStore(DaprClient daprClient, ILogger<DaprSubmissionStore> logger)
    {
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public async Task<SubmissionCreationResult> CreateSubmissionAsync(SubmissionRequest request, string correlationId)
    {
        var idempotencyKey = SubmissionIdempotencyKeys.Build(
            request.Vendor!,
            request.InvoiceNumber!,
            request.Amount);

        var existingTrackingId = await daprClient.GetStateAsync<string>(
            StateStoreName,
            BuildIndexKey(idempotencyKey));

        if (!string.IsNullOrWhiteSpace(existingTrackingId))
        {
            var existingSubmission = await daprClient.GetStateAsync<SubmissionStatusResponse>(
                StateStoreName,
                existingTrackingId);

            if (existingSubmission is not null)
            {
                logger.LogInformation(
                    "Duplicate submission detected for vendor {Vendor}, invoice {InvoiceNumber}, amount {Amount}. Returning existing tracking id {TrackingId}.",
                    request.Vendor,
                    request.InvoiceNumber,
                    request.Amount,
                    existingTrackingId);
                return new SubmissionCreationResult(existingSubmission, true);
            }
        }

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
            await daprClient.SaveStateAsync(StateStoreName, BuildIndexKey(idempotencyKey), trackingId);
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

        return new SubmissionCreationResult(submission, false);
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

    private static string BuildIndexKey(string idempotencyKey) => $"{IndexKeyPrefix}:{idempotencyKey}";
}
