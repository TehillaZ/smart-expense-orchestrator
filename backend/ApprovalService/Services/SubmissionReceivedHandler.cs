using ApprovalService.Models;

namespace ApprovalService.Services;

public sealed class SubmissionReceivedHandler
{
    private readonly ApprovalWorkflowProcessor workflowProcessor;

    public SubmissionReceivedHandler(ApprovalWorkflowProcessor workflowProcessor)
    {
        this.workflowProcessor = workflowProcessor;
    }

    public Task HandleAsync(SubmissionReceivedEvent submissionEvent)
    {
        return workflowProcessor.ProcessAsync(submissionEvent);
    }
}
