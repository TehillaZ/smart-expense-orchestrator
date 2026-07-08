using System.Diagnostics;
using ApprovalFlow.Observability;
using Microsoft.AspNetCore.Builder;

namespace ApprovalFlow.Observability.Tests;

[Collection("Observability")]
public class ObservabilityRegistrationTests
{
    [Fact]
    public void AddApprovalFlowObservability_RegistersActivitySourceForCrossServiceTracing()
    {
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ObservabilityActivitySources.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activities.Add,
        };
        ActivitySource.AddActivityListener(listener);

        var builder = WebApplication.CreateBuilder();
        builder.Configuration["Observability:Enabled"] = "false";
        builder.AddApprovalFlowObservability("TestService");

        var baseline = activities.Count;

        using var activity = ObservabilityActivitySources.Instance.StartActivity("gateway.request");
        using var child = ObservabilityActivitySources.Instance.StartActivity(
            "intake.submission",
            ActivityKind.Internal,
            activity?.Context ?? default);

        var created = activities.Skip(baseline).ToList();
        Assert.Equal(2, created.Count);
        Assert.Equal(activity?.TraceId, child?.TraceId);
    }
}
