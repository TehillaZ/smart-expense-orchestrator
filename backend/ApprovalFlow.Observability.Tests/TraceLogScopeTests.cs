using System.Diagnostics;
using ApprovalFlow.Observability;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApprovalFlow.Observability.Tests;

[Collection("Observability")]
public class TraceLogScopeTests
{
    [Fact]
    public void Begin_IncludesCorrelationIdAndTraceIdInScope()
    {
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ObservabilityActivitySources.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activities.Add,
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = ObservabilityActivitySources.Instance.StartActivity("test.activity");
        Assert.NotNull(activity);

        var logger = new TestLogger();
        using (TraceLogScope.Begin(logger, "corr-123"))
        {
            logger.LogInformation("test");
        }

        Assert.Contains(logger.Scopes, scope =>
            scope.TryGetValue("CorrelationId", out var correlationId)
            && correlationId?.ToString() == "corr-123"
            && scope.TryGetValue("TraceId", out var traceId)
            && traceId?.ToString() == activity.TraceId.ToString());
    }

    private sealed class TestLogger : ILogger
    {
        public List<Dictionary<string, object>> Scopes { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            where TState : notnull
        {
            if (state is IEnumerable<KeyValuePair<string, object>> scopeItems)
            {
                Scopes.Add(scopeItems.ToDictionary(item => item.Key, item => item.Value));
            }

            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
