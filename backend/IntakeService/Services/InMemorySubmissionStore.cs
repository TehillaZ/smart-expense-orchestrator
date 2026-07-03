using System.Collections.Concurrent;
using IntakeService.Models;

namespace IntakeService.Services;

public sealed class InMemorySubmissionStore : ISubmissionStore
{
    private const string InitialStatus = "Received";
    private const string InitialReason = "Invoice received and is pending analysis.";

    private readonly ConcurrentDictionary<string, SubmissionStatusResponse> submissions = new();
    private readonly ConcurrentDictionary<string, string> idempotencyIndex = new(StringComparer.Ordinal);

    public Task<SubmissionCreationResult> CreateSubmissionAsync(SubmissionRequest request, string correlationId)
    {
        var idempotencyKey = SubmissionIdempotencyKeys.Build(
            request.Vendor!,
            request.InvoiceNumber!,
            request.Amount);

        if (idempotencyIndex.TryGetValue(idempotencyKey, out var existingTrackingId)
            && submissions.TryGetValue(existingTrackingId, out var existingSubmission))
        {
            return Task.FromResult(new SubmissionCreationResult(existingSubmission, true));
        }

        var trackingId = Guid.NewGuid().ToString();

        var submission = new SubmissionStatusResponse(
            trackingId,
            correlationId,
            request.Vendor!.Trim(),
            request.InvoiceNumber!.Trim(),
            request.Amount,
            request.Category!.Trim(),
            InitialStatus,
            InitialReason);

        submissions[trackingId] = submission;
        idempotencyIndex[idempotencyKey] = trackingId;

        return Task.FromResult(new SubmissionCreationResult(submission, false));
    }

    public Task<SubmissionStatusResponse?> GetSubmissionAsync(string trackingId)
    {
        return Task.FromResult(submissions.GetValueOrDefault(trackingId));
    }
}
