using System.Diagnostics.Metrics;

namespace ApprovalFlow.Observability;

public sealed class ObservabilityMetrics
{
    public const string MeterName = "ApprovalFlow";

    private readonly Counter<long> requestsTotal;
    private readonly Counter<long> failuresTotal;

    public ObservabilityMetrics()
    {
        var meter = new Meter(MeterName, "1.0.0");
        requestsTotal = meter.CreateCounter<long>(
            "approvalflow.requests.total",
            description: "Total number of instrumented requests.");
        failuresTotal = meter.CreateCounter<long>(
            "approvalflow.requests.failures",
            description: "Total number of failed instrumented requests.");
    }

    public void RecordRequest(string operation, bool succeeded)
    {
        var tags = new KeyValuePair<string, object?>("operation", operation);
        requestsTotal.Add(1, tags);

        if (!succeeded)
        {
            failuresTotal.Add(1, tags);
        }
    }
}
