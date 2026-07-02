using IntakeService.Models;
using IntakeService.Services;

namespace IntakeService.Tests;

public class InMemorySubmissionStoreTests
{
    [Fact]
    public async Task CreateSubmissionAsync_ReturnsReceivedStatusWithGeneratedTrackingId()
    {
        var store = new InMemorySubmissionStore();
        var request = new SubmissionRequest("Acme Corp", "INV-1001", 250m, "Travel");

        var submission = await store.CreateSubmissionAsync(request, "correlation-1");

        Assert.False(string.IsNullOrWhiteSpace(submission.TrackingId));
        Assert.Equal("Received", submission.Status);
        Assert.Equal("Acme Corp", submission.Vendor);
        Assert.Equal("correlation-1", submission.CorrelationId);
    }

    [Fact]
    public async Task GetSubmissionAsync_AfterCreate_ReturnsSameSubmission()
    {
        var store = new InMemorySubmissionStore();
        var request = new SubmissionRequest("Acme Corp", "INV-1002", 75m, "Software");
        var created = await store.CreateSubmissionAsync(request, "correlation-2");

        var fetched = await store.GetSubmissionAsync(created.TrackingId);

        Assert.NotNull(fetched);
        Assert.Equal(created.TrackingId, fetched!.TrackingId);
    }

    [Fact]
    public async Task GetSubmissionAsync_WithUnknownTrackingId_ReturnsNull()
    {
        var store = new InMemorySubmissionStore();

        var fetched = await store.GetSubmissionAsync(Guid.NewGuid().ToString());

        Assert.Null(fetched);
    }
}
