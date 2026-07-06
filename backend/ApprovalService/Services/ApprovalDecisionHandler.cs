using ApprovalService.Models;

namespace ApprovalService.Services;

public sealed class ApprovalDecisionHandler
{
    private readonly ISubmissionStateStore stateStore;
    private readonly IPaymentRequestPublisher paymentRequestPublisher;
    private readonly IMetricsRecorder metricsRecorder;
    private readonly ILogger<ApprovalDecisionHandler> logger;

    public ApprovalDecisionHandler(
        ISubmissionStateStore stateStore,
        IPaymentRequestPublisher paymentRequestPublisher,
        IMetricsRecorder metricsRecorder,
        ILogger<ApprovalDecisionHandler> logger)
    {
        this.stateStore = stateStore;
        this.paymentRequestPublisher = paymentRequestPublisher;
        this.metricsRecorder = metricsRecorder;
        this.logger = logger;
    }

    public async Task<(ApprovalDecisionResponse? Response, IReadOnlyList<string> Errors)> ApplyDecisionAsync(
        string trackingId,
        ApprovalDecisionRequest request)
    {
        var validationErrors = ApprovalDecisionValidator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return (null, validationErrors);
        }

        var state = await stateStore.GetAsync(trackingId);
        if (state is null)
        {
            return (null, ["submission was not found."]);
        }

        if (!string.Equals(state.Status, ApprovalStatuses.Escalated, StringComparison.Ordinal))
        {
            return (null, ["only escalated submissions can receive a human decision."]);
        }

        var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
            ? state.CorrelationId
            : request.CorrelationId.Trim();

        var (status, reason, humanDecision, humanDecisionComment) =
            MapDecision(request.Decision!.Trim(), request.Comment);

        var updated = state with
        {
            Status = status,
            Reason = reason,
            HumanDecision = humanDecision,
            HumanDecisionComment = humanDecisionComment
        };

        await stateStore.SaveAsync(updated);

        if (!string.Equals(status, ApprovalStatuses.Escalated, StringComparison.Ordinal))
        {
            await stateStore.RemoveFromEscalationQueueAsync(trackingId);
        }

        logger.LogInformation(
            "Applied human decision {Decision} to submission {TrackingId}. New status {Status}.",
            request.Decision,
            trackingId,
            status);

        if (!string.Equals(request.Decision, "RequestInfo", StringComparison.OrdinalIgnoreCase))
        {
            await ApprovalMetricsRecorder.RecordHumanDecisionAsync(
                metricsRecorder,
                updated,
                request.Decision!.Trim());
        }

        if (string.Equals(status, ApprovalStatuses.HumanApproved, StringComparison.Ordinal))
        {
            await paymentRequestPublisher.PublishAsync(updated);
        }

        return (new ApprovalDecisionResponse(trackingId, status, correlationId, reason), []);
    }

    private static (string Status, string Reason, string HumanDecision, string? HumanDecisionComment) MapDecision(
        string decision,
        string? comment)
    {
        var trimmedComment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

        return decision switch
        {
            "Approve" => (
                ApprovalStatuses.HumanApproved,
                trimmedComment is null
                    ? "Expense approved by a human reviewer."
                    : $"Expense approved by a human reviewer. Note: {trimmedComment}",
                "Approve",
                trimmedComment),
            "Reject" => (
                ApprovalStatuses.Rejected,
                trimmedComment is null
                    ? "Expense rejected by a human reviewer."
                    : $"Expense rejected by a human reviewer. Note: {trimmedComment}",
                "Reject",
                trimmedComment),
            "RequestInfo" => (
                ApprovalStatuses.Escalated,
                trimmedComment is null
                    ? "More information was requested before a final decision."
                    : $"More information requested: {trimmedComment}",
                "RequestInfo",
                trimmedComment),
            _ => throw new InvalidOperationException($"Unsupported decision: {decision}")
        };
    }
}
