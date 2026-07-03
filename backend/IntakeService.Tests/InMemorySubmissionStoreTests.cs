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

        var result = await store.CreateSubmissionAsync(request, "correlation-1");
        var submission = result.Submission;

        Assert.False(result.IsDuplicate);

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
        var submission = created.Submission;

        var fetched = await store.GetSubmissionAsync(submission.TrackingId);

        Assert.NotNull(fetched);
        Assert.Equal(created.Submission.TrackingId, fetched!.TrackingId);
    }

    [Fact]
    public async Task CreateSubmissionAsync_WithDuplicateVendorInvoiceAndAmount_ReturnsExistingTrackingId()
    {
        var store = new InMemorySubmissionStore();
        var request = new SubmissionRequest("Acme Corp", "INV-1007", 120m, "Meals");

        var first = await store.CreateSubmissionAsync(request, "correlation-a");
        var duplicate = await store.CreateSubmissionAsync(request, "correlation-b");

        Assert.False(first.IsDuplicate);
        Assert.True(duplicate.IsDuplicate);
        Assert.Equal(first.Submission.TrackingId, duplicate.Submission.TrackingId);
        Assert.Equal("correlation-a", duplicate.Submission.CorrelationId);
    }

    [Fact]
    public async Task CreateSubmissionAsync_WithDifferentAmount_CreatesNewSubmission()
    {
        var store = new InMemorySubmissionStore();

        var first = await store.CreateSubmissionAsync(
            new SubmissionRequest("Acme Corp", "INV-1007", 120m, "Meals"),
            "correlation-a");
        var second = await store.CreateSubmissionAsync(
            new SubmissionRequest("Acme Corp", "INV-1007", 121m, "Meals"),
            "correlation-b");

        Assert.False(first.IsDuplicate);
        Assert.False(second.IsDuplicate);
        Assert.NotEqual(first.Submission.TrackingId, second.Submission.TrackingId);
    }

    [Fact]
    public async Task GetSubmissionAsync_WithUnknownTrackingId_ReturnsNull()
    {
        var store = new InMemorySubmissionStore();

        var fetched = await store.GetSubmissionAsync(Guid.NewGuid().ToString());

        Assert.Null(fetched);
    }
}
