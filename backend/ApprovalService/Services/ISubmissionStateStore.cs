using ApprovalService.Models;

namespace ApprovalService.Services;

public interface ISubmissionStateStore
{
    Task<SubmissionState?> GetAsync(string trackingId);

    Task SaveAsync(SubmissionState state);
}
