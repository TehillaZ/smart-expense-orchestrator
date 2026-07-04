using NotificationService.Models;
using NotificationService.Services;

namespace NotificationService.Routes;

public static class NotificationRoutes
{
    public static IEndpointRouteBuilder MapNotificationRoutes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/notifications/{trackingId}", async (
            string trackingId,
            INotificationStore notificationStore,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("NotificationRoutes");

            try
            {
                var completed = await notificationStore.GetAsync(
                    trackingId,
                    NotificationOutcomeTypes.PaymentCompleted);
                if (completed is not null)
                {
                    return Results.Ok(ToResponse(completed));
                }

                var failed = await notificationStore.GetAsync(
                    trackingId,
                    NotificationOutcomeTypes.PaymentFailed);
                if (failed is not null)
                {
                    return Results.Ok(ToResponse(failed));
                }

                return Results.NotFound(new { message = "No notification was recorded for this submission yet." });
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to read notification for submission {TrackingId}.",
                    trackingId);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetNotification");

        return app;
    }

    private static NotificationResponse ToResponse(NotificationRecord record) =>
        new(
            record.TrackingId,
            record.CorrelationId,
            record.Status,
            record.Reason,
            record.Amount,
            record.OutcomeType,
            record.Channel,
            record.SentAt);
}
