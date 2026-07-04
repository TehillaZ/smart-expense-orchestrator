using PaymentService.Models;
using PaymentService.Services;

namespace PaymentService.Routes;

public static class InternalPaymentRoutes
{
    public static IEndpointRouteBuilder MapInternalPaymentRoutes(this IEndpointRouteBuilder app)
    {
        app.MapPost("/internal/payments/process", async (
            PaymentRequestedEvent paymentEvent,
            PaymentRequestedHandler handler,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("InternalPaymentRoutes");

            try
            {
                await handler.HandleAsync(paymentEvent);
                logger.LogInformation(
                    "Processed payment request for submission {TrackingId} through direct service invocation.",
                    paymentEvent.TrackingId);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to process payment request for submission {TrackingId} through direct service invocation.",
                    paymentEvent.TrackingId);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("ProcessPaymentDirect");

        return app;
    }
}
