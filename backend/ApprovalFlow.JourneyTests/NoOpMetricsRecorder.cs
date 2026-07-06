using ApprovalService.Models;
using ApprovalService.Services;

namespace ApprovalFlow.JourneyTests;

internal sealed class NoOpApprovalMetricsRecorder : IMetricsRecorder
{
    public Task RecordAsync(MetricsRecordRequest request) => Task.CompletedTask;
}

internal sealed class NoOpPaymentMetricsRecorder : PaymentService.Services.IMetricsRecorder
{
    public Task RecordAsync(PaymentService.Models.MetricsRecordRequest request) => Task.CompletedTask;
}
