using ApprovalService.Models;
using ApprovalService.Services;

namespace ApprovalService.Routes;

public static class InternalApprovalRoutes
{
    public static IEndpointRouteBuilder MapInternalApprovalRoutes(this IEndpointRouteBuilder app)
    {
        app.MapPost("/internal/submissions/process", async (
            SubmissionReceivedEvent submissionEvent,
            SubmissionReceivedHandler handler,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("InternalApprovalRoutes");

            try
            {
                await handler.HandleAsync(submissionEvent);
                logger.LogInformation(
                    "Processed submission {TrackingId} through direct service invocation.",
                    submissionEvent.TrackingId);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to process submission {TrackingId} through direct service invocation.",
                    submissionEvent.TrackingId);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("ProcessSubmissionDirect");

        return app;
    }
}
