using IntakeService.Models;

namespace IntakeService.Services;

public interface ISubmissionStore
{
    Task<SubmissionStatusResponse> CreateSubmissionAsync(SubmissionRequest request, string correlationId);

    Task<SubmissionStatusResponse?> GetSubmissionAsync(string trackingId);
}
