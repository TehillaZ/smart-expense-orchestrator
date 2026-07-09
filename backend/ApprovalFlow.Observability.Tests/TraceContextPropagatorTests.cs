using System.Diagnostics;
using ApprovalFlow.Observability;

namespace ApprovalFlow.Observability.Tests;

[Collection("Observability")]
public class TraceContextPropagatorTests
{
    [Fact]
    public void CaptureCurrent_ReturnsActivityId()
    {
        using var listener = CreateListener();
        using var activity = ObservabilityActivitySources.Instance.StartActivity("parent");
        Assert.NotNull(activity);

        var captured = TraceContextPropagator.CaptureCurrent();

        Assert.Equal(activity.Id, captured);
    }

    [Fact]
    public void StartConsumerActivity_LinksChildToParentTrace()
    {
        using var listener = CreateListener(out var activities);
        using var parent = ObservabilityActivitySources.Instance.StartActivity("parent");
        Assert.NotNull(parent);

        var parentId = TraceContextPropagator.CaptureCurrent();
        parent.Stop();

        using (TraceContextPropagator.StartConsumerActivity(
            "pubsub.submission.received",
            parentId,
            "corr-42"))
        {
            var child = Activity.Current;
            Assert.NotNull(child);
            Assert.Equal(parent.TraceId, child.TraceId);
            Assert.Equal("corr-42", child.GetTagItem("correlation.id"));
        }

        Assert.Contains(activities, activity => activity.OperationName == "pubsub.submission.received");
    }

    [Fact]
    public void CorrelationIdResolver_ReusesIncomingHeader()
    {
        Assert.Equal("incoming-id", CorrelationIdResolver.Resolve("incoming-id"));
        Assert.False(string.IsNullOrWhiteSpace(CorrelationIdResolver.Resolve(null)));
    }

    private static ActivityListener CreateListener() => CreateListener(out _);

    private static ActivityListener CreateListener(out List<Activity> activities)
    {
        var captured = new List<Activity>();
        activities = captured;
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ObservabilityActivitySources.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = captured.Add,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
