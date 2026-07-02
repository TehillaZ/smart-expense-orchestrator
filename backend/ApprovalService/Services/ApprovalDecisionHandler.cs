using ApprovalService.Models;

namespace ApprovalService.Services;

public sealed class ApprovalDecisionHandler
{
    private readonly ISubmissionStateStore stateStore;
    private readonly ILogger<ApprovalDecisionHandler> logger;

    public ApprovalDecisionHandler(ISubmissionStateStore stateStore, ILogger<ApprovalDecisionHandler> logger)
    {
        this.stateStore = stateStore;
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

        var (status, reason) = MapDecision(request.Decision!.Trim(), request.Comment);

        var updated = state with
        {
            Status = status,
            Reason = reason
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

        return (new ApprovalDecisionResponse(trackingId, status, correlationId, reason), []);
    }

    private static (string Status, string Reason) MapDecision(string decision, string? comment)
    {
        return decision switch
        {
            "Approve" => (
                ApprovalStatuses.HumanApproved,
                string.IsNullOrWhiteSpace(comment)
                    ? "Expense approved by a human reviewer."
                    : $"Expense approved by a human reviewer. Note: {comment.Trim()}"),
            "Reject" => (
                ApprovalStatuses.Rejected,
                string.IsNullOrWhiteSpace(comment)
                    ? "Expense rejected by a human reviewer."
                    : $"Expense rejected by a human reviewer. Note: {comment.Trim()}"),
            "RequestInfo" => (
                ApprovalStatuses.Escalated,
                string.IsNullOrWhiteSpace(comment)
                    ? "More information was requested before a final decision."
                    : $"More information requested: {comment.Trim()}"),
            _ => throw new InvalidOperationException($"Unsupported decision: {decision}")
        };
    }
}
