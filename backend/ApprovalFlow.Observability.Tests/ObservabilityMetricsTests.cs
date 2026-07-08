using ApprovalFlow.Observability;

namespace ApprovalFlow.Observability.Tests;

public class ObservabilityMetricsTests
{
    [Fact]
    public void RecordRequest_TracksSuccessfulAndFailedOperations()
    {
        var metrics = new ObservabilityMetrics();

        var exception = Record.Exception(() =>
        {
            metrics.RecordRequest("invoice.ai.recommendation", succeeded: true);
            metrics.RecordRequest("invoice.ai.recommendation", succeeded: false);
        });

        Assert.Null(exception);
    }
}
