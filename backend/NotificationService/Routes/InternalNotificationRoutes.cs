using NotificationService.Models;
using NotificationService.Services;

namespace NotificationService.Routes;

public static class InternalNotificationRoutes
{
    public static IEndpointRouteBuilder MapInternalNotificationRoutes(this IEndpointRouteBuilder app)
    {
        app.MapPost("/internal/notifications/payment-completed", async (
            PaymentCompletedEvent paymentEvent,
            NotificationProcessor processor,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("InternalNotificationRoutes");

            try
            {
                await processor.ProcessPaymentCompletedAsync(paymentEvent);
                logger.LogInformation(
                    "Recorded payment completion notification for submission {TrackingId}.",
                    paymentEvent.TrackingId);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to record payment completion notification for submission {TrackingId}.",
                    paymentEvent.TrackingId);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("ProcessPaymentCompletedDirect");

        app.MapPost("/internal/notifications/payment-failed", async (
            PaymentFailedEvent paymentEvent,
            NotificationProcessor processor,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("InternalNotificationRoutes");

            try
            {
                await processor.ProcessPaymentFailedAsync(paymentEvent);
                logger.LogInformation(
                    "Recorded payment failure notification for submission {TrackingId}.",
                    paymentEvent.TrackingId);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to record payment failure notification for submission {TrackingId}.",
                    paymentEvent.TrackingId);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("ProcessPaymentFailedDirect");

        return app;
    }
}
