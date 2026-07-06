using Dapr.Client;
using PaymentService.Models;

namespace PaymentService.Services;

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

public static class PaymentMetricsRecorder
{
    public static Task RecordPaidAsync(
        IMetricsRecorder metricsRecorder,
        SubmissionWorkflowState submission,
        string approvalOutcomeStatus)
    {
        var decisionType = string.Equals(
            approvalOutcomeStatus,
            ApprovalOutcomeStatuses.HumanApproved,
            StringComparison.Ordinal)
            ? "HumanApproved"
            : "AutoApproved";

        return metricsRecorder.RecordAsync(new MetricsRecordRequest(
            submission.TrackingId,
            submission.CorrelationId,
            MetricsEventTypes.Paid,
            PaymentStatuses.Paid,
            submission.Amount,
            decisionType,
            submission.Vendor,
            submission.InvoiceNumber));
    }

    public static Task RecordPaymentFailedAsync(
        IMetricsRecorder metricsRecorder,
        SubmissionWorkflowState submission,
        string finalStatus)
    {
        return metricsRecorder.RecordAsync(new MetricsRecordRequest(
            submission.TrackingId,
            submission.CorrelationId,
            MetricsEventTypes.PaymentFailed,
            finalStatus,
            submission.Amount,
            "PaymentFailed",
            submission.Vendor,
            submission.InvoiceNumber));
    }
}
