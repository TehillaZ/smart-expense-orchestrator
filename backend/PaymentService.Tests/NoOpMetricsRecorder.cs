using PaymentService.Models;
using PaymentService.Services;

namespace PaymentService.Tests;

internal sealed class NoOpMetricsRecorder : IMetricsRecorder
{
    public Task RecordAsync(MetricsRecordRequest request) => Task.CompletedTask;
}
