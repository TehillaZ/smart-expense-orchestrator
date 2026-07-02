using System.Collections.Concurrent;
using IntakeService.Models;

namespace IntakeService.Services;

public sealed class InMemorySubmissionStore : ISubmissionStore
{
    private const string InitialStatus = "Received";
    private const string InitialReason = "Invoice received and is pending analysis.";

    private readonly ConcurrentDictionary<string, SubmissionStatusResponse> submissions = new();

    public Task<SubmissionStatusResponse> CreateSubmissionAsync(SubmissionRequest request, string correlationId)
    {
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
        return Task.FromResult(submission);
    }

    public Task<SubmissionStatusResponse?> GetSubmissionAsync(string trackingId)
    {
        return Task.FromResult(submissions.GetValueOrDefault(trackingId));
    }
}
