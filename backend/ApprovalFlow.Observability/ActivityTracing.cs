using System.Diagnostics;

namespace ApprovalFlow.Observability;

public static class ActivityTracing
{
    public static async Task<T> TraceAsync<T>(
        string activityName,
        Func<Task<T>> action,
        IEnumerable<KeyValuePair<string, object?>>? tags = null,
        ObservabilityMetrics? metrics = null)
    {
        using var activity = ObservabilityActivitySources.Instance.StartActivity(
            activityName,
            ActivityKind.Client);

        if (tags is not null)
        {
            foreach (var tag in tags)
            {
                activity?.SetTag(tag.Key, tag.Value);
            }
        }

        try
        {
            var result = await action();
            activity?.SetStatus(ActivityStatusCode.Ok);
            metrics?.RecordRequest(activityName, succeeded: true);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.message", ex.Message);
            metrics?.RecordRequest(activityName, succeeded: false);
            throw;
        }
    }
}
