using IntakeService.Models;

namespace IntakeService.Services;

public interface IDashboardMetricsStore
{
    Task<DashboardReadModel> GetReadModelAsync();

    Task ApplyEventAsync(MetricsRecordRequest request);
}
