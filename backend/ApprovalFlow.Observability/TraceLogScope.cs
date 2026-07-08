using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ApprovalFlow.Observability;

public static class TraceLogScope
{
    public static IDisposable Begin(ILogger logger, string? correlationId)
    {
        var scope = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            scope["CorrelationId"] = correlationId;
        }

        var activity = Activity.Current;
        if (activity is not null)
        {
            scope["TraceId"] = activity.TraceId.ToString();
            scope["SpanId"] = activity.SpanId.ToString();
        }

        return logger.BeginScope(scope) ?? NullScope.Instance;
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
