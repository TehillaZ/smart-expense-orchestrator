using IntakeService.Models;

namespace IntakeService.Services;

public interface ISubmissionStore
{
    Task<SubmissionCreationResult> CreateSubmissionAsync(SubmissionRequest request, string correlationId);

    Task<SubmissionStatusResponse?> GetSubmissionAsync(string trackingId);

    Task<SubmissionStatusResponse?> FindByCorrelationIdAsync(string correlationId);

    Task<IReadOnlyList<SubmissionStatusResponse>> ListSubmissionsAsync();
}
