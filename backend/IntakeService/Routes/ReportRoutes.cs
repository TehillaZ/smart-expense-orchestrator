using IntakeService.Models;
using IntakeService.Services;

namespace IntakeService.Routes;

public static class ReportRoutes
{
    public static IEndpointRouteBuilder MapReportRoutes(this IEndpointRouteBuilder app)
    {
        app.MapGet("/reports/summary", async (
            IDashboardMetricsStore metricsStore,
            ISubmissionStore submissionStore,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("ReportRoutes");

            try
            {
                var readModel = await metricsStore.GetReadModelAsync();
                var enrichedActivity = await EnrichRecentActivityAsync(
                    submissionStore,
                    readModel.RecentActivity);
                var enrichedModel = readModel with { RecentActivity = enrichedActivity };
                var summary = DashboardMetricsProjector.ToReportingSummary(enrichedModel);

                logger.LogInformation(
                    "Returned pre-aggregated reporting summary for {TotalSubmissions} submissions. AutoApprovalRate={AutoApprovalRate}, EscalationRate={EscalationRate}.",
                    summary.TotalSubmissions,
                    summary.AutoApprovalRate,
                    summary.EscalationRate);

                return Results.Ok(summary);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to read pre-aggregated reporting summary.");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetReportingSummary");

        return app;
    }

    private static async Task<IReadOnlyList<RecentActivityItem>> EnrichRecentActivityAsync(
        ISubmissionStore submissionStore,
        IReadOnlyList<RecentActivityItem> recentActivity)
    {
        if (recentActivity.Count == 0)
        {
            return recentActivity;
        }

        var enriched = new List<RecentActivityItem>(recentActivity.Count);

        foreach (var item in recentActivity)
        {
            var submission = await submissionStore.GetSubmissionAsync(item.TrackingId);
            if (submission is null)
            {
                enriched.Add(item);
                continue;
            }

            var (status, decisionType) = SubmissionStateResolver.ToRecentActivityFields(submission);
            enriched.Add(item with
            {
                Status = status,
                DecisionType = decisionType,
                Amount = submission.Amount,
                Vendor = submission.Vendor,
                InvoiceNumber = submission.InvoiceNumber,
            });
        }

        return enriched;
    }
}
