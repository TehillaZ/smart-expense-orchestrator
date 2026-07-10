using System.Net;
using System.Text;
using ApprovalFlow.PolicyRag;
using ApprovalService.Models;
using ApprovalService.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ApprovalService.Tests;

public class GroqInvoiceAiProviderTests
{
    [Fact]
    public async Task GetRecommendationAsync_ParsesStructuredProviderResponse()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "choices": [
                        {
                          "message": {
                            "content": "{\"recommendation\":\"Escalate\",\"confidence\":0.91,\"citedRules\":[\"AUTONOMY-CEILING\"],\"reasoningSummary\":\"Above ceiling.\"}"
                          }
                        }
                      ]
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            });

        var provider = CreateProvider(handler, apiKey: "test-key");
        var submission = new SubmissionReceivedEvent(
            "tracking-1",
            "correlation-1",
            "Acme",
            "INV-1",
            900m,
            "meals");

        var result = await provider.GetRecommendationAsync(submission);

        Assert.Equal("Escalate", result.Recommendation);
        Assert.Equal(0.91m, result.Confidence);
        Assert.Contains("AUTONOMY-CEILING", result.CitedRules);
        Assert.Contains("Above ceiling", result.ReasoningSummary);
        Assert.Equal(1, handler.CallCount);
        Assert.Contains("Bearer", handler.LastAuthorizationHeader ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetRecommendationAsync_WithoutApiKey_FailsCleanly()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var provider = CreateProvider(handler, apiKey: null);
        var submission = new SubmissionReceivedEvent(
            "tracking-1",
            "correlation-1",
            "Acme",
            "INV-1",
            50m,
            "meals");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetRecommendationAsync(submission));

        Assert.Contains("ApiKey", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task GetRecommendationAsync_OnHttpError_FailsCleanly()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent("upstream failed", Encoding.UTF8, "text/plain")
            });
        var provider = CreateProvider(handler, apiKey: "test-key");
        var submission = new SubmissionReceivedEvent(
            "tracking-1",
            "correlation-1",
            "Acme",
            "INV-1",
            50m,
            "meals");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetRecommendationAsync(submission));

        Assert.Contains("502", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, handler.CallCount);
    }

    private static GroqInvoiceAiProvider CreateProvider(StubHttpMessageHandler handler, string? apiKey)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.groq.com/openai/v1/")
        };
        var apiKeyAccessor = new LlmApiKeyAccessor();
        apiKeyAccessor.SetApiKey(apiKey);

        return new GroqInvoiceAiProvider(
            httpClient,
            new FakePolicyRetriever(),
            apiKeyAccessor,
            Options.Create(new LlmProviderOptions
            {
                Provider = LlmProviderNames.Groq,
                Model = "llama-3.3-70b-versatile",
                BaseUrl = "https://api.groq.com/openai/v1",
                TimeoutSeconds = 10
            }),
            NullLogger<GroqInvoiceAiProvider>.Instance);
    }

    private sealed class FakePolicyRetriever : IPolicyRetriever
    {
        public Task<PolicyRetrievalResult> RetrieveAsync(
            string category,
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            var clause = new PolicyClause(
                "AUTONOMY-CEILING",
                "Amounts above ceiling escalate.",
                "Autonomy",
                ["meals"]);
            return Task.FromResult(new PolicyRetrievalResult([clause], "AUTONOMY-CEILING: Amounts above ceiling escalate."));
        }
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            this.responder = responder;
        }

        public int CallCount { get; private set; }

        public string? LastAuthorizationHeader { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastAuthorizationHeader = request.Headers.Authorization?.ToString();
            return Task.FromResult(responder(request));
        }
    }
}
