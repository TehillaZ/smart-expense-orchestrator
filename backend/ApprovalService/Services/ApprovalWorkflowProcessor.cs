using ApprovalService.Models;

namespace ApprovalService.Services;

public sealed class ApprovalWorkflowProcessor
{
    private const string ProcessingReason = "Submission received by Approval Service and is under policy review.";

    private readonly ISubmissionStateStore stateStore;
    private readonly IPolicySettingsStore policySettingsStore;
    private readonly IInvoiceAiProvider aiProvider;
    private readonly PolicyEngine policyEngine;
    private readonly ILogger<ApprovalWorkflowProcessor> logger;

    public ApprovalWorkflowProcessor(
        ISubmissionStateStore stateStore,
        IPolicySettingsStore policySettingsStore,
        IInvoiceAiProvider aiProvider,
        PolicyEngine policyEngine,
        ILogger<ApprovalWorkflowProcessor> logger)
    {
        this.stateStore = stateStore;
        this.policySettingsStore = policySettingsStore;
        this.aiProvider = aiProvider;
        this.policyEngine = policyEngine;
        this.logger = logger;
    }

    public async Task ProcessAsync(SubmissionReceivedEvent submissionEvent)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = submissionEvent.CorrelationId
        });

        var existing = await stateStore.GetAsync(submissionEvent.TrackingId);

        var processingState = existing is null
            ? new SubmissionState(
                submissionEvent.TrackingId,
                submissionEvent.CorrelationId,
                submissionEvent.Vendor,
                submissionEvent.InvoiceNumber,
                submissionEvent.Amount,
                submissionEvent.Category,
                ApprovalStatuses.Processing,
                ProcessingReason)
            : existing with
            {
                Status = ApprovalStatuses.Processing,
                Reason = ProcessingReason
            };

        await stateStore.SaveAsync(processingState);

        try
        {
            var settings = await policySettingsStore.GetAsync();
            var aiRecommendation = await aiProvider.GetRecommendationAsync(submissionEvent);
            var evaluation = policyEngine.Evaluate(submissionEvent, settings, aiRecommendation);

            var finalState = processingState with
            {
                Status = evaluation.FinalStatus,
                Reason = evaluation.Reason,
                AiRecommendation = evaluation.AiRecommendation.Recommendation,
                AiConfidence = evaluation.AiRecommendation.Confidence,
                CitedRules = evaluation.AiRecommendation.CitedRules,
                PolicyVersion = settings.PolicyVersion
            };

            await stateStore.SaveAsync(finalState);

            if (string.Equals(finalState.Status, ApprovalStatuses.Escalated, StringComparison.Ordinal))
            {
                await stateStore.AddToEscalationQueueAsync(finalState.TrackingId);
            }

            logger.LogInformation(
                "Submission {TrackingId} evaluated to {Status}. Correlation {CorrelationId}.",
                submissionEvent.TrackingId,
                finalState.Status,
                submissionEvent.CorrelationId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Policy evaluation failed for submission {TrackingId}. Escalating for human review.",
                submissionEvent.TrackingId);

            var failedState = processingState with
            {
                Status = ApprovalStatuses.Escalated,
                Reason = "Policy evaluation failed and the submission was escalated for human review.",
                AiRecommendation = "Escalate",
                AiConfidence = 0m,
                CitedRules = ["POLICY-ERROR"]
            };

            await stateStore.SaveAsync(failedState);
            await stateStore.AddToEscalationQueueAsync(failedState.TrackingId);
        }
    }
}
