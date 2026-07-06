using IntakeService.Models;
using IntakeService.Services;

namespace IntakeService.Routes;

public static class AuditRoutes
{
    public static IEndpointRouteBuilder MapAuditRoutes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/audit/{trackingId}", async (
            string trackingId,
            ISubmissionStore store,
            DaprNotificationLookup notificationLookup,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("AuditRoutes");

            try
            {
                var submission = await store.GetSubmissionAsync(trackingId);
                if (submission is null)
                {
                    logger.LogWarning(
                        "Audit trail requested for unknown submission {TrackingId}.",
                        trackingId);
                    return Results.NotFound(new { message = "Submission was not found." });
                }

                using var scope = logger.BeginScope(new Dictionary<string, object>
                {
                    ["CorrelationId"] = submission.CorrelationId,
                    ["TrackingId"] = trackingId
                });

                var notification = await notificationLookup.GetAsync(trackingId);
                var auditTrail = AuditTrailBuilder.Build(submission, notification);

                logger.LogInformation(
                    "Returned audit trail for submission {TrackingId} with {EventCount} timeline events.",
                    trackingId,
                    auditTrail.Timeline.Count);

                return Results.Ok(auditTrail);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to build audit trail for submission {TrackingId}.", trackingId);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetAuditTrail");

        return app;
    }
}
