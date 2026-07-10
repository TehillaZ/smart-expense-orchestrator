using ApprovalService.Models;

namespace ApprovalService.Services;

/// <summary>
/// Retries the primary LLM on transient failures, then optionally calls a fallback provider once.
/// If both fail, the exception propagates and ApprovalWorkflowProcessor escalates to human review.
/// </summary>
public sealed class ResilientInvoiceAiProvider : IInvoiceAiProvider
{
    private readonly IInvoiceAiProvider primaryProvider;
    private readonly IInvoiceAiProvider? fallbackProvider;
    private readonly string primaryName;
    private readonly string? fallbackName;
    private readonly int retryCount;
    private readonly int retryDelayMs;
    private readonly ILogger<ResilientInvoiceAiProvider> logger;

    public ResilientInvoiceAiProvider(
        IInvoiceAiProvider primaryProvider,
        string primaryName,
        IInvoiceAiProvider? fallbackProvider,
        string? fallbackName,
        int retryCount,
        int retryDelayMs,
        ILogger<ResilientInvoiceAiProvider> logger)
    {
        this.primaryProvider = primaryProvider;
        this.primaryName = primaryName;
        this.fallbackProvider = fallbackProvider;
        this.fallbackName = fallbackName;
        this.retryCount = Math.Max(0, retryCount);
        this.retryDelayMs = Math.Clamp(retryDelayMs, 0, 30_000);
        this.logger = logger;
    }

    public async Task<AiRecommendationResult> GetRecommendationAsync(
        SubmissionReceivedEvent submission,
        CancellationToken cancellationToken = default)
    {
        var maxAttempts = retryCount + 1;
        Exception? primaryFailure = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await primaryProvider.GetRecommendationAsync(submission, cancellationToken);
            }
            catch (Exception ex) when (attempt < maxAttempts && IsRetryable(ex))
            {
                primaryFailure = ex;
                logger.LogWarning(
                    ex,
                    "Primary LLM provider {Provider} failed for submission {TrackingId} (attempt {Attempt}/{MaxAttempts}). Retrying.",
                    primaryName,
                    submission.TrackingId,
                    attempt,
                    maxAttempts);

                if (retryDelayMs > 0)
                {
                    await Task.Delay(retryDelayMs, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                primaryFailure = ex;
                logger.LogWarning(
                    ex,
                    "Primary LLM provider {Provider} failed for submission {TrackingId} after {Attempt} attempt(s).",
                    primaryName,
                    submission.TrackingId,
                    attempt);
                break;
            }
        }

        if (fallbackProvider is null || string.IsNullOrWhiteSpace(fallbackName))
        {
            throw primaryFailure
                ?? new InvalidOperationException($"Primary LLM provider {primaryName} failed without an exception.");
        }

        logger.LogWarning(
            "Falling back to LLM provider {FallbackProvider} for submission {TrackingId} after primary {Provider} failed.",
            fallbackName,
            submission.TrackingId,
            primaryName);

        try
        {
            return await fallbackProvider.GetRecommendationAsync(submission, cancellationToken);
        }
        catch (Exception fallbackFailure)
        {
            logger.LogError(
                fallbackFailure,
                "Fallback LLM provider {FallbackProvider} also failed for submission {TrackingId}.",
                fallbackName,
                submission.TrackingId);

            throw new AggregateException(
                $"Primary LLM '{primaryName}' and fallback LLM '{fallbackName}' both failed.",
                primaryFailure
                    ?? new InvalidOperationException($"Primary LLM provider {primaryName} failed."),
                fallbackFailure);
        }
    }

    public static bool IsRetryable(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            switch (current)
            {
                case HttpRequestException:
                case TaskCanceledException:
                case TimeoutException:
                case IOException:
                    return true;
            }

            if (current is InvalidOperationException
                && ContainsAny(current.Message, "429", "502", "503", "504", "timeout", "SSL", "certificate"))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsAny(string message, params string[] tokens)
    {
        foreach (var token in tokens)
        {
            if (message.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
