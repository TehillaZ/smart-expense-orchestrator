using ApprovalService.Models;
using ApprovalService.Services;

namespace ApprovalService.Tests;

internal sealed class NoOpMetricsRecorder : IMetricsRecorder
{
    public Task RecordAsync(MetricsRecordRequest request) => Task.CompletedTask;
}
