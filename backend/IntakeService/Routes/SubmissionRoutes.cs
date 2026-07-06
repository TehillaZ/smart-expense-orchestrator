using Dapr.Client;
using IntakeService.Models;
using IntakeService.Services;

namespace IntakeService.Routes;

public static class SubmissionRoutes
{
    private const string PubSubName = "order-pubsub";
    private const string SubmissionReceivedTopic = "submission.received";

    public static IEndpointRouteBuilder MapSubmissionRoutes(this IEndpointRouteBuilder app)
    {
        app.MapPost("/submissions", async (
            SubmissionRequest request,
            ISubmissionStore store,
            IDashboardMetricsStore metricsStore,
            DaprClient daprClient,
            ILoggerFactory loggerFactory) =>
        {
            var correlationId = Guid.NewGuid().ToString();
            var logger = loggerFactory.CreateLogger("SubmissionRoutes");

            using var scope = logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            });

            var errors = SubmissionValidator.Validate(request);
            if (errors.Count > 0)
            {
                logger.LogWarning(
                    "Submission validation failed for correlation {CorrelationId} with {ErrorCount} validation errors.",
                    correlationId,
                    errors.Count);
                return Results.BadRequest(new ValidationErrorResponse(errors));
            }

            var creation = await store.CreateSubmissionAsync(request, correlationId);
            var submission = creation.Submission;
            // the idempotency check is done here
            if (!creation.IsDuplicate)
            {
                var submissionEvent = new SubmissionReceivedEvent(
                    submission.TrackingId,
                    submission.CorrelationId,
                    submission.Vendor,
                    submission.InvoiceNumber,
                    submission.Amount,
                    submission.Category);

                var published = false;

                try
                {
                    await daprClient.PublishEventAsync(
                        PubSubName,
                        SubmissionReceivedTopic,
                        submissionEvent);
                    published = true;

                    logger.LogInformation(
                        "Published {Topic} for submission {TrackingId} with correlation {CorrelationId}.",
                        SubmissionReceivedTopic,
                        submission.TrackingId,
                        correlationId);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Failed to publish {Topic} event for submission {TrackingId} with correlation {CorrelationId}.",
                        SubmissionReceivedTopic,
                        submission.TrackingId,
                        correlationId);
                }

                if (!published)
                {
                    try
                    {
                        await daprClient.InvokeMethodAsync(
                            HttpMethod.Post,
                            DaprServiceIds.ApprovalService,
                            "internal/submissions/process",
                            submissionEvent);

                        logger.LogWarning(
                            "Used direct approval invocation fallback for submission {TrackingId} with correlation {CorrelationId}.",
                            submission.TrackingId,
                            correlationId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            ex,
                            "Failed to invoke direct approval processing for submission {TrackingId} with correlation {CorrelationId}.",
                            submission.TrackingId,
                            correlationId);
                    }
                }

                try
                {
                    await DashboardMetricsRecorder.RecordSubmissionCreatedAsync(metricsStore, submission);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Failed to record dashboard metrics for new submission {TrackingId} with correlation {CorrelationId}.",
                        submission.TrackingId,
                        correlationId);
                }
            }
            else
            {
                logger.LogInformation(
                    "Skipped publishing {Topic} for duplicate submission {TrackingId}.",
                    SubmissionReceivedTopic,
                    submission.TrackingId);
            }

            logger.LogInformation(
                creation.IsDuplicate
                    ? "Returned existing submission {TrackingId} for duplicate vendor {Vendor} with correlation {CorrelationId}."
                    : "Accepted submission {TrackingId} for vendor {Vendor} with correlation {CorrelationId}.",
                submission.TrackingId,
                submission.Vendor,
                correlationId);

            return Results.Accepted(
                $"/submissions/{submission.TrackingId}",
                new SubmissionAcceptedResponse(
                    submission.TrackingId,
                    submission.CorrelationId,
                    creation.IsDuplicate));
        })
        .WithName("CreateSubmission");

        app.MapGet("/submissions", async (
            ISubmissionStore store,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SubmissionRoutes");

            try
            {
                var submissions = await store.ListSubmissionsAsync();
                logger.LogInformation("Returned {Count} submissions from persistent state.", submissions.Count);
                return Results.Ok(submissions);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to list submissions from persistent state.");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("ListSubmissions");

        app.MapGet("/submissions/{trackingId}", async (
            string trackingId,
            ISubmissionStore store,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("SubmissionRoutes");
            var submission = await store.GetSubmissionAsync(trackingId);

            if (submission is null)
            {
                logger.LogWarning("Submission {TrackingId} was not found.", trackingId);
                return Results.NotFound(new { message = "Submission was not found." });
            }

            using var scope = logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = submission.CorrelationId
            });

            logger.LogInformation(
                "Returned status {Status} for submission {TrackingId} with correlation {CorrelationId}.",
                submission.Status,
                trackingId,
                submission.CorrelationId);
            return Results.Ok(submission);
        })
        .WithName("GetSubmissionStatus");

        return app;
    }
}
