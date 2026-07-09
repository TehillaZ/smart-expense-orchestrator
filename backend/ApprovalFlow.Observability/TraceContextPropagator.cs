using System.Diagnostics;

namespace ApprovalFlow.Observability;

public static class CorrelationIdResolver
{
    public static string Resolve(string? headerValue)
    {
        return string.IsNullOrWhiteSpace(headerValue)
            ? Guid.NewGuid().ToString()
            : headerValue.Trim();
    }
}

public static class TraceContextPropagator
{
    public static string? CaptureCurrent()
    {
        var activity = Activity.Current;
        if (activity is null || string.IsNullOrWhiteSpace(activity.Id))
        {
            return null;
        }

        return activity.Id;
    }

    public static IDisposable StartConsumerActivity(
        string activityName,
        string? traceParent,
        string? correlationId = null)
    {
        ActivityContext parentContext = default;
        var hasParent = !string.IsNullOrWhiteSpace(traceParent)
            && ActivityContext.TryParse(traceParent, traceState: null, out parentContext);

        var activity = hasParent
            ? ObservabilityActivitySources.Instance.StartActivity(
                activityName,
                ActivityKind.Consumer,
                parentContext)
            : ObservabilityActivitySources.Instance.StartActivity(
                activityName,
                ActivityKind.Consumer);

        if (activity is not null && !string.IsNullOrWhiteSpace(correlationId))
        {
            activity.SetTag("correlation.id", correlationId);
            activity.AddBaggage(CorrelationConstants.HeaderName, correlationId);
        }

        return (IDisposable?)activity ?? NullScope.Instance;
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
