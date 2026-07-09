using ApprovalFlow.Observability;
using Dapr;
using NotificationService.Models;
using NotificationService.Services;

namespace NotificationService.Routes;

public static class SubscriptionRoutes
{
    private const string PubSubName = "order-pubsub";
    private const string PaymentCompletedTopic = "payment.completed";
    private const string PaymentFailedTopic = "payment.failed";

    public static IEndpointRouteBuilder MapSubscriptionRoutes(this IEndpointRouteBuilder app)
    {
        app.MapPost("/events/payment-completed", [Topic(PubSubName, PaymentCompletedTopic)] async (
            PaymentCompletedEvent paymentEvent,
            NotificationProcessor processor,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SubscriptionRoutes");

            using var activityScope = TraceContextPropagator.StartConsumerActivity(
                "pubsub.payment.completed",
                paymentEvent.TraceParent,
                paymentEvent.CorrelationId);
            using var logScope = TraceLogScope.Begin(logger, paymentEvent.CorrelationId);

            try
            {
                await processor.ProcessPaymentCompletedAsync(paymentEvent);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to process {Topic} event for submission {TrackingId}.",
                    PaymentCompletedTopic,
                    paymentEvent.TrackingId);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("HandlePaymentCompleted");

        app.MapPost("/events/payment-failed", [Topic(PubSubName, PaymentFailedTopic)] async (
            PaymentFailedEvent paymentEvent,
            NotificationProcessor processor,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SubscriptionRoutes");

            using var activityScope = TraceContextPropagator.StartConsumerActivity(
                "pubsub.payment.failed",
                paymentEvent.TraceParent,
                paymentEvent.CorrelationId);
            using var logScope = TraceLogScope.Begin(logger, paymentEvent.CorrelationId);

            try
            {
                await processor.ProcessPaymentFailedAsync(paymentEvent);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to process {Topic} event for submission {TrackingId}.",
                    PaymentFailedTopic,
                    paymentEvent.TrackingId);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("HandlePaymentFailed");

        return app;
    }
}
