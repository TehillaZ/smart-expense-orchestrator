using Dapr;
using PaymentService.Models;
using PaymentService.Services;

namespace PaymentService.Routes;

public static class SubscriptionRoutes
{
    private const string PubSubName = "order-pubsub";
    private const string PaymentRequestedTopic = "payment.requested";

    public static IEndpointRouteBuilder MapSubscriptionRoutes(this IEndpointRouteBuilder app)
    {
        app.MapPost("/events/payment-requested", [Topic(PubSubName, PaymentRequestedTopic)] async (
            PaymentRequestedEvent paymentEvent,
            PaymentRequestedHandler handler,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SubscriptionRoutes");

            try
            {
                await handler.HandleAsync(paymentEvent);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to process {Topic} event for submission {TrackingId}.",
                    PaymentRequestedTopic,
                    paymentEvent.TrackingId);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("HandlePaymentRequested");

        return app;
    }
}
