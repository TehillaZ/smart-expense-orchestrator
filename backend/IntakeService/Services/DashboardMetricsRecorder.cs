using IntakeService.Models;
using IntakeService.Services;

namespace IntakeService.Services;

public static class DashboardMetricsRecorder
{
    public static Task RecordSubmissionCreatedAsync(
        IDashboardMetricsStore metricsStore,
        SubmissionStatusResponse submission)
    {
        return metricsStore.ApplyEventAsync(new MetricsRecordRequest(
            submission.TrackingId,
            submission.CorrelationId,
            MetricsEventTypes.SubmissionCreated,
            submission.Status,
            submission.Amount,
            "Received",
            submission.Vendor,
            submission.InvoiceNumber));
    }
}
