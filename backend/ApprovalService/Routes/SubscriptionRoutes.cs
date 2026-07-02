using ApprovalService.Models;
using ApprovalService.Services;
using Dapr;

namespace ApprovalService.Routes;

public static class SubscriptionRoutes
{
    private const string PubSubName = "order-pubsub";
    private const string SubmissionReceivedTopic = "submission.received";

    public static IEndpointRouteBuilder MapSubscriptionRoutes(this IEndpointRouteBuilder app)
    {
        app.MapPost("/events/submission-received", [Topic(PubSubName, SubmissionReceivedTopic)] async (
            SubmissionReceivedEvent submissionEvent,
            SubmissionReceivedHandler handler,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SubscriptionRoutes");

            try
            {
                await handler.HandleAsync(submissionEvent);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to process {Topic} event for submission {TrackingId}.",
                    SubmissionReceivedTopic,
                    submissionEvent.TrackingId);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("HandleSubmissionReceived");

        return app;
    }
}
