using ApprovalService.Models;

namespace ApprovalService.Services;

public sealed class SubmissionReceivedHandler
{
    private const string ProcessingStatus = "Processing";
    private const string ProcessingReason = "Submission received by Approval Service and is under policy review.";

    private readonly ISubmissionStateStore stateStore;
    private readonly ILogger<SubmissionReceivedHandler> logger;

    public SubmissionReceivedHandler(ISubmissionStateStore stateStore, ILogger<SubmissionReceivedHandler> logger)
    {
        this.stateStore = stateStore;
        this.logger = logger;
    }

    public async Task HandleAsync(SubmissionReceivedEvent submissionEvent)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = submissionEvent.CorrelationId
        });

        var existing = await stateStore.GetAsync(submissionEvent.TrackingId);

        var updated = existing is null
            ? new SubmissionState(
                submissionEvent.TrackingId,
                submissionEvent.CorrelationId,
                submissionEvent.Vendor,
                submissionEvent.InvoiceNumber,
                submissionEvent.Amount,
                submissionEvent.Category,
                ProcessingStatus,
                ProcessingReason)
            : existing with { Status = ProcessingStatus, Reason = ProcessingReason };

        await stateStore.SaveAsync(updated);

        logger.LogInformation(
            "Submission {TrackingId} moved to {Status} after being received by Approval Service. Correlation {CorrelationId}.",
            submissionEvent.TrackingId,
            ProcessingStatus,
            submissionEvent.CorrelationId);
    }
}
