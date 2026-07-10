using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApprovalFlow.PolicyRag;
using ApprovalService.Models;
using Microsoft.Extensions.Options;

namespace ApprovalService.Services;

/// <summary>
/// OpenAI-compatible chat completions client for Groq (https://api.groq.com/openai/v1/).
/// Useful when Gemini HTTPS fails under local SSL inspection.
/// </summary>
public sealed class GroqInvoiceAiProvider : IInvoiceAiProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient httpClient;
    private readonly IPolicyRetriever policyRetriever;
    private readonly ILlmApiKeyAccessor apiKeyAccessor;
    private readonly LlmProviderOptions options;
    private readonly ILogger<GroqInvoiceAiProvider> logger;

    public GroqInvoiceAiProvider(
        HttpClient httpClient,
        IPolicyRetriever policyRetriever,
        ILlmApiKeyAccessor apiKeyAccessor,
        IOptions<LlmProviderOptions> options,
        ILogger<GroqInvoiceAiProvider> logger)
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
                "Groq provider is configured but LlmProvider:ApiKey is missing. " +
                "Set the key in the Dapr secret store or fail over to Provider=Stub.");
        }

        var retrieval = await policyRetriever.RetrieveAsync(
            submission.Category,
            submission.Amount,
            cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(
            BuildRequestBody(submission, retrieval.PromptContext),
            options: SerializerOptions);

        logger.LogInformation(
            "Calling Groq model {Model} for submission {TrackingId} with correlation {CorrelationId}.",
            options.Model,
            submission.TrackingId,
            submission.CorrelationId);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Groq provider returned {(int)response.StatusCode} {response.ReasonPhrase}. Body: {Truncate(responseBody)}");
        }

        var completion = JsonSerializer.Deserialize<GroqChatCompletionResponse>(responseBody, SerializerOptions)
            ?? throw new InvalidOperationException("Groq provider returned an empty response body.");

        var content = completion.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Groq provider returned no assistant content.");
        }

        return ParseRecommendation(content, retrieval.Clauses.Select(clause => clause.RuleId).ToList());
    }

    private object BuildRequestBody(SubmissionReceivedEvent submission, string policyContext)
    {
        var systemPrompt =
            "You are an expense-policy assistant. Recommend Approve, Reject, Escalate, or RequestInfo. " +
            "Respond with ONLY compact JSON: " +
            "{\"recommendation\":\"Approve|Reject|Escalate|RequestInfo\",\"confidence\":0.0,\"citedRules\":[\"RULE-ID\"],\"reasoningSummary\":\"...\"}. " +
            "Use only the provided policy clauses. Never invent approvals above policy.";

        var userPrompt =
            $"Submission trackingId={submission.TrackingId}, vendor={submission.Vendor}, " +
            $"invoice={submission.InvoiceNumber}, amount={submission.Amount}, category={submission.Category}.\n" +
            $"Policy clauses:\n{policyContext}";

        return new
        {
            model = string.IsNullOrWhiteSpace(options.Model) ? "llama-3.3-70b-versatile" : options.Model.Trim(),
            temperature = 0,
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };
    }

    private static AiRecommendationResult ParseRecommendation(string content, IReadOnlyList<string> fallbackRules)
    {
        var json = ExtractJsonObject(content);
        GroqRecommendationPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<GroqRecommendationPayload>(json, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Groq provider returned non-JSON recommendation content: {Truncate(content)}",
                ex);
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.Recommendation))
        {
            throw new InvalidOperationException(
                $"Groq provider returned an incomplete recommendation payload: {Truncate(content)}");
        }

        var recommendation = NormalizeRecommendation(payload.Recommendation);
        var confidence = Math.Clamp(payload.Confidence, 0m, 1m);
        var citedRules = payload.CitedRules is { Count: > 0 }
            ? payload.CitedRules
            : fallbackRules;
        var reasoning = string.IsNullOrWhiteSpace(payload.ReasoningSummary)
            ? "Groq recommendation received without a reasoning summary."
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
                $"Groq provider returned unsupported recommendation '{recommendation}'.")
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
                $"Groq provider response did not contain a JSON object: {Truncate(content)}");
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

    private sealed class GroqChatCompletionResponse
    {
        public IReadOnlyList<GroqChoice>? Choices { get; init; }
    }

    private sealed class GroqChoice
    {
        public GroqMessage? Message { get; init; }
    }

    private sealed class GroqMessage
    {
        public string? Content { get; init; }
    }

    private sealed class GroqRecommendationPayload
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
