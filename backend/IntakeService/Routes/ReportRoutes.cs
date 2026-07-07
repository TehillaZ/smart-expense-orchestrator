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

        app.MapGet("/reports/compliance", async (
            ISubmissionStore submissionStore,
            DaprPolicyLookup policyLookup,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("ReportRoutes");

            try
            {
                var submissions = await submissionStore.ListSubmissionsAsync();
                var policy = await policyLookup.GetAsync();
                var report = ComplianceReportBuilder.Build(
                    submissions,
                    policy.AutonomyCeiling,
                    policy.PolicyVersion);

                logger.LogInformation(
                    "Returned compliance report for {AutoApprovedReviewedCount} auto-approved submissions with {ViolationCount} violations.",
                    report.AutoApprovedReviewedCount,
                    report.ViolationCount);

                return Results.Ok(report);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to build auto-approval compliance report.");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("GetComplianceReport");

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
