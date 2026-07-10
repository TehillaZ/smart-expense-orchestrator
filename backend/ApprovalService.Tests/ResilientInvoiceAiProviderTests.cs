using ApprovalService.Models;
using ApprovalService.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApprovalService.Tests;

public class ResilientInvoiceAiProviderTests
{
    [Fact]
    public async Task GetRecommendationAsync_RetriesTransientPrimaryFailure_ThenSucceeds()
    {
        var primary = new FakeProvider();
        primary.Failures.Enqueue(new HttpRequestException("connection reset"));
        primary.Results.Enqueue(BuildResult("Approve"));

        var provider = CreateProvider(primary, fallback: null, retryCount: 1);
        var result = await provider.GetRecommendationAsync(BuildSubmission());

        Assert.Equal("Approve", result.Recommendation);
        Assert.Equal(2, primary.CallCount);
    }

    [Fact]
    public async Task GetRecommendationAsync_FallsBackWhenPrimaryExhausted()
    {
        var primary = new FakeProvider();
        primary.Failures.Enqueue(new HttpRequestException("ssl failed"));
        primary.Failures.Enqueue(new HttpRequestException("ssl failed again"));

        var fallback = new FakeProvider();
        fallback.Results.Enqueue(BuildResult("Escalate"));

        var provider = CreateProvider(primary, fallback, retryCount: 1, fallbackName: "Groq");
        var result = await provider.GetRecommendationAsync(BuildSubmission());

        Assert.Equal("Escalate", result.Recommendation);
        Assert.Equal(2, primary.CallCount);
        Assert.Equal(1, fallback.CallCount);
    }

    [Fact]
    public async Task GetRecommendationAsync_SkipsRetryForNonTransient_AndFallsBack()
    {
        var primary = new FakeProvider();
        primary.Failures.Enqueue(new InvalidOperationException("ApiKey is missing"));

        var fallback = new FakeProvider();
        fallback.Results.Enqueue(BuildResult("Reject"));

        var provider = CreateProvider(primary, fallback, retryCount: 2, fallbackName: "Groq");
        var result = await provider.GetRecommendationAsync(BuildSubmission());

        Assert.Equal("Reject", result.Recommendation);
        Assert.Equal(1, primary.CallCount);
        Assert.Equal(1, fallback.CallCount);
    }

    [Fact]
    public async Task GetRecommendationAsync_WhenPrimaryAndFallbackFail_ThrowsAggregate()
    {
        var primary = new FakeProvider();
        primary.Failures.Enqueue(new HttpRequestException("primary down"));

        var fallback = new FakeProvider();
        fallback.Failures.Enqueue(new HttpRequestException("fallback down"));

        var provider = CreateProvider(primary, fallback, retryCount: 0, fallbackName: "Groq");

        var exception = await Assert.ThrowsAsync<AggregateException>(
            () => provider.GetRecommendationAsync(BuildSubmission()));

        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.Equal(1, primary.CallCount);
        Assert.Equal(1, fallback.CallCount);
    }

    [Theory]
    [InlineData(typeof(HttpRequestException), true)]
    [InlineData(typeof(TaskCanceledException), true)]
    [InlineData(typeof(InvalidOperationException), false)]
    public void IsRetryable_ClassifiesExceptions(Type exceptionType, bool expected)
    {
        Exception exception = exceptionType.Name switch
        {
            nameof(HttpRequestException) => new HttpRequestException("boom"),
            nameof(TaskCanceledException) => new TaskCanceledException(),
            _ => new InvalidOperationException("bad payload")
        };

        Assert.Equal(expected, ResilientInvoiceAiProvider.IsRetryable(exception));
    }

    [Fact]
    public void IsRetryable_TreatsProviderHttpStatusMessagesAsRetryable()
    {
        Assert.True(ResilientInvoiceAiProvider.IsRetryable(new InvalidOperationException("Groq returned 429")));
        Assert.True(ResilientInvoiceAiProvider.IsRetryable(new InvalidOperationException("SSL certificate")));
    }

    private static ResilientInvoiceAiProvider CreateProvider(
        IInvoiceAiProvider primary,
        IInvoiceAiProvider? fallback,
        int retryCount,
        string? fallbackName = null)
    {
        return new ResilientInvoiceAiProvider(
            primary,
            "Gemini",
            fallback,
            fallbackName,
            retryCount,
            retryDelayMs: 0,
            NullLogger<ResilientInvoiceAiProvider>.Instance);
    }

    private static SubmissionReceivedEvent BuildSubmission() =>
        new("tracking-1", "correlation-1", "Acme", "INV-1", 100m, "meals");

    private static AiRecommendationResult BuildResult(string recommendation) =>
        new(recommendation, 0.9m, ["RULE-1"], "ok");

    private sealed class FakeProvider : IInvoiceAiProvider
    {
        public Queue<Exception> Failures { get; } = new();
        public Queue<AiRecommendationResult> Results { get; } = new();
        public int CallCount { get; private set; }

        public Task<AiRecommendationResult> GetRecommendationAsync(
            SubmissionReceivedEvent submission,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            if (Failures.Count > 0)
            {
                throw Failures.Dequeue();
            }

            if (Results.Count == 0)
            {
                throw new InvalidOperationException("No fake result configured.");
            }

            return Task.FromResult(Results.Dequeue());
        }
    }
}
