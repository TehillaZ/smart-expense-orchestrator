using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApprovalFlow.PolicyRag;
using ApprovalService.Models;
using Microsoft.Extensions.Options;

namespace ApprovalService.Services;

public sealed class GeminiInvoiceAiProvider : IInvoiceAiProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient httpClient;
    private readonly IPolicyRetriever policyRetriever;
    private readonly ILlmApiKeyAccessor apiKeyAccessor;
    private readonly LlmProviderOptions options;
    private readonly ILogger<GeminiInvoiceAiProvider> logger;

    public GeminiInvoiceAiProvider(
        HttpClient httpClient,
        IPolicyRetriever policyRetriever,
        ILlmApiKeyAccessor apiKeyAccessor,
        IOptions<LlmProviderOptions> options,
        ILogger<GeminiInvoiceAiProvider> logger)
    {
        this.httpClient = httpClient;
        this.policyRetriever = policyRetriever;
        this.apiKeyAccessor = apiKeyAccessor;
        this.options = options.Value;
        this.logger = logger;
    }

    public async Task<AiRecommendationResult> GetRecommendationAsync(
        SubmissionReceivedEvent submission,
        CancellationToken cancellationToken = default)
    {
        var apiKey = apiKeyAccessor.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Gemini provider is configured but LlmProvider:ApiKey is missing. " +
                "Set the key in the Dapr secret store or fail over to Provider=Stub.");
        }

        var retrieval = await policyRetriever.RetrieveAsync(
            submission.Category,
            submission.Amount,
            cancellationToken);

        var model = string.IsNullOrWhiteSpace(options.Model) ? "gemini-2.0-flash" : options.Model.Trim();
        var requestUri = $"models/{model}:generateContent?key={Uri.EscapeDataString(apiKey)}";

        logger.LogInformation(
            "Calling Gemini model {Model} for submission {TrackingId} with correlation {CorrelationId}.",
            model,
            submission.TrackingId,
            submission.CorrelationId);

        using var response = await httpClient.PostAsJsonAsync(
            requestUri,
            BuildRequestBody(submission, retrieval.PromptContext),
            SerializerOptions,
            cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Gemini provider returned {(int)response.StatusCode} {response.ReasonPhrase}. Body: {Truncate(responseBody)}");
        }

        var completion = JsonSerializer.Deserialize<GeminiGenerateContentResponse>(responseBody, SerializerOptions)
            ?? throw new InvalidOperationException("Gemini provider returned an empty response body.");

        var content = completion.Candidates?
            .SelectMany(candidate => candidate.Content?.Parts ?? [])
            .Select(part => part.Text)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Gemini provider returned no assistant content.");
        }

        return ParseRecommendation(content, retrieval.Clauses.Select(clause => clause.RuleId).ToList());
    }

    private static object BuildRequestBody(SubmissionReceivedEvent submission, string policyContext)
    {
        var prompt =
            "You are an expense-policy assistant. Recommend Approve, Reject, Escalate, or RequestInfo. " +
            "Respond with ONLY compact JSON: " +
            "{\"recommendation\":\"Approve|Reject|Escalate|RequestInfo\",\"confidence\":0.0,\"citedRules\":[\"RULE-ID\"],\"reasoningSummary\":\"...\"}. " +
            "Use only the provided policy clauses. Never invent approvals above policy.\n\n" +
            $"Submission trackingId={submission.TrackingId}, vendor={submission.Vendor}, " +
            $"invoice={submission.InvoiceNumber}, amount={submission.Amount}, category={submission.Category}.\n" +
            $"Policy clauses:\n{policyContext}";

        return new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0,
                responseMimeType = "application/json"
            }
        };
    }

    private static AiRecommendationResult ParseRecommendation(string content, IReadOnlyList<string> fallbackRules)
    {
        var json = ExtractJsonObject(content);
        GeminiRecommendationPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<GeminiRecommendationPayload>(json, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Gemini provider returned non-JSON recommendation content: {Truncate(content)}",
                ex);
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.Recommendation))
        {
            throw new InvalidOperationException(
                $"Gemini provider returned an incomplete recommendation payload: {Truncate(content)}");
        }

        var recommendation = NormalizeRecommendation(payload.Recommendation);
        var confidence = Math.Clamp(payload.Confidence, 0m, 1m);
        var citedRules = payload.CitedRules is { Count: > 0 }
            ? payload.CitedRules
            : fallbackRules;
        var reasoning = string.IsNullOrWhiteSpace(payload.ReasoningSummary)
            ? "Gemini recommendation received without a reasoning summary."
            : payload.ReasoningSummary.Trim();

        return new AiRecommendationResult(recommendation, confidence, citedRules, reasoning);
    }

    private static string NormalizeRecommendation(string recommendation)
    {
        var normalized = recommendation.Trim();
        return normalized.ToUpperInvariant() switch
        {
            "APPROVE" => "Approve",
            "REJECT" => "Reject",
            "ESCALATE" => "Escalate",
            "REQUESTINFO" or "REQUEST_INFO" or "REQUEST MORE INFO" => "RequestInfo",
            _ => throw new InvalidOperationException(
                $"Gemini provider returned unsupported recommendation '{recommendation}'.")
        };
    }

    private static string ExtractJsonObject(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewline >= 0 && lastFence > firstNewline)
            {
                trimmed = trimmed[(firstNewline + 1)..lastFence].Trim();
            }
        }

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            throw new InvalidOperationException(
                $"Gemini provider response did not contain a JSON object: {Truncate(content)}");
        }

        return trimmed[start..(end + 1)];
    }

    private static string Truncate(string value, int maxLength = 400)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "...";
    }

    private sealed class GeminiGenerateContentResponse
    {
        public IReadOnlyList<GeminiCandidate>? Candidates { get; init; }
    }

    private sealed class GeminiCandidate
    {
        public GeminiContent? Content { get; init; }
    }

    private sealed class GeminiContent
    {
        public IReadOnlyList<GeminiPart>? Parts { get; init; }
    }

    private sealed class GeminiPart
    {
        public string? Text { get; init; }
    }

    private sealed class GeminiRecommendationPayload
    {
        [JsonPropertyName("recommendation")]
        public string? Recommendation { get; init; }

        [JsonPropertyName("confidence")]
        public decimal Confidence { get; init; }

        [JsonPropertyName("citedRules")]
        public IReadOnlyList<string>? CitedRules { get; init; }

        [JsonPropertyName("reasoningSummary")]
        public string? ReasoningSummary { get; init; }
    }
}
