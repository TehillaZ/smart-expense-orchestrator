using IntakeService.Models;
using IntakeService.Services;

namespace IntakeService.Routes;

public static class InternalMetricsRoutes
{
    public static IEndpointRouteBuilder MapInternalMetricsRoutes(this IEndpointRouteBuilder app)
    {
        app.MapPost("/internal/metrics/record", async (
            MetricsRecordRequest request,
            IDashboardMetricsStore metricsStore,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("InternalMetricsRoutes");

            using var scope = logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = request.CorrelationId,
                ["TrackingId"] = request.TrackingId
            });

            try
            {
                await metricsStore.ApplyEventAsync(request);
                return Results.Ok();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to apply dashboard metrics event {EventType} for submission {TrackingId}.",
                    request.EventType,
                    request.TrackingId);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("RecordDashboardMetricsEvent");

        return app;
    }
}
