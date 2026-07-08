using System.Diagnostics;
using ApprovalFlow.Observability;
using ApprovalFlow.PolicyRag.Tests;
using ApprovalService.Models;
using ApprovalService.Services;

namespace ApprovalFlow.Observability.Tests;

[Collection("Observability")]
public class TracedInvoiceAiProviderTests
{
    [Fact]
    public async Task GetRecommendationAsync_CreatesAiSpanWithSubmissionTags()
    {
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ObservabilityActivitySources.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activities.Add,
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new TracedInvoiceAiProvider(
            new StubInvoiceAiProvider(PolicyRagTestSupport.CreateRetriever()),
            new ObservabilityMetrics());

        var submission = new SubmissionReceivedEvent(
            "tracking-1",
            "correlation-1",
            "Acme",
            "INV-1",
            100m,
            "meals");

        await provider.GetRecommendationAsync(submission);

        var aiSpan = Assert.Single(activities, activity => activity.OperationName == TracedInvoiceAiProvider.ActivityName);
        Assert.Equal(TracedInvoiceAiProvider.ActivityName, aiSpan.OperationName);
        Assert.Equal("tracking-1", aiSpan.GetTagItem("submission.tracking_id"));
        Assert.Equal("meals", aiSpan.GetTagItem("submission.category"));
        Assert.Equal(ActivityStatusCode.Ok, aiSpan.Status);
    }
}
