using ApprovalService.Models;
using ApprovalService.Services;

namespace ApprovalService.Routes;

public static class ApprovalRoutes
{
    public static IEndpointRouteBuilder MapApprovalRoutes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/approvals/queue", (ILoggerFactory loggerFactory) =>
        {
            var correlationId = Guid.NewGuid().ToString();
            var logger = loggerFactory.CreateLogger("ApprovalRoutes");

            using var scope = logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            });

            logger.LogInformation("Returned empty approval queue placeholder with correlation {CorrelationId}.", correlationId);
            return Results.Ok(Array.Empty<object>());
        })
        .WithName("GetApprovalQueue");

        app.MapPost("/approvals/{trackingId}/decision", (
            string trackingId,
            ApprovalDecisionRequest request,
            ILoggerFactory loggerFactory) =>
        {
            var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
                ? Guid.NewGuid().ToString()
                : request.CorrelationId.Trim();

            var logger = loggerFactory.CreateLogger("ApprovalRoutes");

            using var scope = logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            });

            var errors = ApprovalDecisionValidator.Validate(request);
            if (errors.Count > 0)
            {
                logger.LogWarning(
                    "Approval decision validation failed for {TrackingId} with correlation {CorrelationId}.",
                    trackingId,
                    correlationId);
                return Results.BadRequest(new ValidationErrorResponse(errors));
            }

            logger.LogInformation(
                "Accepted placeholder approval decision {Decision} for {TrackingId} with correlation {CorrelationId}.",
                request.Decision,
                trackingId,
                correlationId);

            return Results.Ok(new ApprovalDecisionResponse(trackingId, "DecisionAccepted", correlationId));
        })
        .WithName("SubmitApprovalDecision");

        return app;
    }
}
