using ApprovalFlow.Reliability.Throttling;

namespace ApprovalFlow.Reliability.Tests;

public class BulkheadGateTests
{
    [Fact]
    public async Task ExecuteAsync_LimitsConcurrentWork()
    {
        using var bulkhead = new BulkheadGate(maxConcurrency: 1);
        var gate = new object();
        var concurrent = 0;
        var maxConcurrent = 0;

        async Task RunWork()
        {
            await bulkhead.ExecuteAsync(async () =>
            {
                lock (gate)
                {
                    concurrent++;
                    maxConcurrent = Math.Max(maxConcurrent, concurrent);
                }

                await Task.Delay(50);

                lock (gate)
                {
                    concurrent--;
                }
            });
        }

        var first = RunWork();
        await Task.Delay(10);
        var second = RunWork();
        await Task.WhenAll(first, second);

        Assert.Equal(1, maxConcurrent);
    }
}
