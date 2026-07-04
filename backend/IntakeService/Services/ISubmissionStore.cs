using IntakeService.Models;

namespace IntakeService.Services;

public interface ISubmissionStore
{
    Task<SubmissionCreationResult> CreateSubmissionAsync(SubmissionRequest request, string correlationId);

    Task<SubmissionStatusResponse?> GetSubmissionAsync(string trackingId);

    Task<IReadOnlyList<SubmissionStatusResponse>> ListSubmissionsAsync();
}
