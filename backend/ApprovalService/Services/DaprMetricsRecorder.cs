using ApprovalService.Models;
using Dapr.Client;

namespace ApprovalService.Services;

public interface IMetricsRecorder
{
    Task RecordAsync(MetricsRecordRequest request);
}

public sealed class DaprMetricsRecorder : IMetricsRecorder
{
    private readonly DaprClient daprClient;
    private readonly ILogger<DaprMetricsRecorder> logger;

    public DaprMetricsRecorder(DaprClient daprClient, ILogger<DaprMetricsRecorder> logger)
    {
        this.daprClient = daprClient;
        this.logger = logger;
    }

    public async Task RecordAsync(MetricsRecordRequest request)
    {
        try
        {
            await daprClient.InvokeMethodAsync(
                HttpMethod.Post,
                DaprServiceIds.IntakeService,
                "internal/metrics/record",
                request);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to record dashboard metrics event {EventType} for submission {TrackingId} with correlation {CorrelationId}.",
                request.EventType,
                request.TrackingId,
                request.CorrelationId);
        }
    }
}

public static class ApprovalMetricsRecorder
{
    public static Task RecordPolicyOutcomeAsync(
        IMetricsRecorder metricsRecorder,
        SubmissionState state)
    {
        var eventType = string.Equals(state.Status, ApprovalStatuses.AutoApproved, StringComparison.Ordinal)
            ? MetricsEventTypes.AutoApproved
            : MetricsEventTypes.Escalated;

        var decisionType = string.Equals(eventType, MetricsEventTypes.AutoApproved, StringComparison.Ordinal)
            ? "AutoApproved"
            : "Escalated";

        return metricsRecorder.RecordAsync(new MetricsRecordRequest(
            state.TrackingId,
            state.CorrelationId,
            eventType,
            state.Status,
            state.Amount,
            decisionType,
            state.Vendor,
            state.InvoiceNumber));
    }

    public static Task RecordHumanDecisionAsync(
        IMetricsRecorder metricsRecorder,
        SubmissionState state,
        string decision)
    {
        var eventType = string.Equals(decision, "Approve", StringComparison.OrdinalIgnoreCase)
            ? MetricsEventTypes.HumanApproved
            : MetricsEventTypes.Rejected;

        var decisionType = string.Equals(decision, "Approve", StringComparison.OrdinalIgnoreCase)
            ? "HumanApproved"
            : "Rejected";

        return metricsRecorder.RecordAsync(new MetricsRecordRequest(
            state.TrackingId,
            state.CorrelationId,
            eventType,
            state.Status,
            state.Amount,
            decisionType,
            state.Vendor,
            state.InvoiceNumber));
    }
}
