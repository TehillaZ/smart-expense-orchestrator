using System.Net;
using System.Text;
using ApprovalFlow.PolicyRag;
using ApprovalService.Models;
using ApprovalService.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ApprovalService.Tests;

public class GeminiInvoiceAiProviderTests
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
                      "candidates": [
                        {
                          "content": {
                            "parts": [
                              {
                                "text": "{\"recommendation\":\"Escalate\",\"confidence\":0.91,\"citedRules\":[\"AUTONOMY-CEILING\"],\"reasoningSummary\":\"Above ceiling.\"}"
                              }
                            ]
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
        Assert.Equal(1, handler.CallCount);
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

    private static GeminiInvoiceAiProvider CreateProvider(StubHttpMessageHandler handler, string? apiKey)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/")
        };
        var apiKeyAccessor = new LlmApiKeyAccessor();
        apiKeyAccessor.SetApiKey(apiKey);

        return new GeminiInvoiceAiProvider(
            httpClient,
            new FakePolicyRetriever(),
            apiKeyAccessor,
            Options.Create(new LlmProviderOptions
            {
                Provider = LlmProviderNames.Gemini,
                Model = "gemini-2.0-flash",
                BaseUrl = "https://generativelanguage.googleapis.com/v1beta/",
                TimeoutSeconds = 10
            }),
            NullLogger<GeminiInvoiceAiProvider>.Instance);
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

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(responder(request));
        }
    }
}
