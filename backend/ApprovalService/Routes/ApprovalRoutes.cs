using ApprovalService.Models;
using ApprovalService.Services;

namespace ApprovalService.Routes;

public static class ApprovalRoutes
{
    public static IEndpointRouteBuilder MapApprovalRoutes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/approvals/queue", async (
            ISubmissionStateStore stateStore,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("ApprovalRoutes");

            try
            {
                var escalated = await stateStore.GetEscalatedSubmissionsAsync();
                var queue = escalated
                    .Select(state => new EscalationQueueItem(
                        state.TrackingId,
                        state.Vendor,
                        state.InvoiceNumber,
                        state.Amount,
                        state.Category,
                        state.AiRecommendation ?? "Escalate",
                        state.AiConfidence ?? 0m,
                        state.CitedRules ?? [],
                        state.Reason))
                    .ToArray();

                logger.LogInformation("Returned {Count} escalated submissions.", queue.Length);
                return Results.Ok(queue);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load escalation queue.");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetApprovalQueue");

        app.MapPost("/approvals/{trackingId}/decision", async (
            string trackingId,
            ApprovalDecisionRequest request,
            ApprovalDecisionHandler decisionHandler,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("ApprovalRoutes");

            try
            {
                var (response, errors) = await decisionHandler.ApplyDecisionAsync(trackingId, request);
                if (errors.Count > 0)
                {
                    logger.LogWarning(
                        "Approval decision failed for {TrackingId} with {ErrorCount} errors.",
                        trackingId,
                        errors.Count);
                    return errors.Contains("submission was not found.")
                        ? Results.NotFound(new ValidationErrorResponse(errors))
                        : Results.BadRequest(new ValidationErrorResponse(errors));
                }

                using var scope = logger.BeginScope(new Dictionary<string, object>
                {
                    ["CorrelationId"] = response!.CorrelationId
                });

                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to apply approval decision for {TrackingId}.", trackingId);
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("SubmitApprovalDecision");

        return app;
    }
}
