using ApprovalFlow.Observability;
using ApprovalFlow.PolicyRag;
using ApprovalFlow.Reliability.Throttling;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ApprovalService.Services;

public static class InvoiceAiProviderRegistration
{
    public const string OpenRouterHttpClientName = "openrouter-invoice-ai";
    public const string GeminiHttpClientName = "gemini-invoice-ai";
    public const string GroqHttpClientName = "groq-invoice-ai";
    public const string OpenRouterFallbackHttpClientName = "openrouter-invoice-ai-fallback";
    public const string GeminiFallbackHttpClientName = "gemini-invoice-ai-fallback";
    public const string GroqFallbackHttpClientName = "groq-invoice-ai-fallback";

    public static IServiceCollection AddConfiguredInvoiceAiProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<LlmProviderOptions>(
            configuration.GetSection(LlmProviderOptions.SectionName));
        services.Configure<RateLimitOptions>(
            configuration.GetSection(RateLimitOptions.SectionName));
        services.TryAddSingleton<ObservabilityMetrics>();
        services.TryAddSingleton<LlmApiKeyAccessor>();
        services.AddPolicyRag(options =>
        {
            var policyPath = configuration["PolicyRag:PolicyDocumentPath"] ?? "Data/policy.md";
            options.PolicyDocumentPath = policyPath;
        });

        var options = configuration
            .GetSection(LlmProviderOptions.SectionName)
            .Get<LlmProviderOptions>() ?? new LlmProviderOptions();

        var primaryName = string.IsNullOrWhiteSpace(options.Provider)
            ? LlmProviderNames.Stub
            : options.Provider.Trim();
        var fallbackName = string.IsNullOrWhiteSpace(options.FallbackProvider)
            ? null
            : options.FallbackProvider.Trim();

        ValidateProviderName(primaryName);
        if (fallbackName is not null)
        {
            ValidateProviderName(fallbackName);
            if (string.Equals(primaryName, fallbackName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"{LlmProviderOptions.SectionName}:FallbackProvider must differ from Provider.");
            }
        }

        RegisterHttpClient(services, primaryName, isFallback: false);
        if (fallbackName is not null)
        {
            RegisterHttpClient(services, fallbackName, isFallback: true);
        }

        services.AddSingleton<StubInvoiceAiProvider>();
        services.AddSingleton<IInvoiceAiProvider>(serviceProvider =>
        {
            var llmOptions = serviceProvider.GetRequiredService<IOptions<LlmProviderOptions>>().Value;
            var primary = CreateProviderInstance(
                serviceProvider,
                primaryName,
                isFallback: false,
                llmOptions);

            IInvoiceAiProvider resilient = primary;
            if (fallbackName is not null)
            {
                var fallback = CreateProviderInstance(
                    serviceProvider,
                    fallbackName,
                    isFallback: true,
                    llmOptions);
                resilient = new ResilientInvoiceAiProvider(
                    primary,
                    primaryName,
                    fallback,
                    fallbackName,
                    llmOptions.RetryCount,
                    llmOptions.RetryDelayMs,
                    serviceProvider.GetRequiredService<ILogger<ResilientInvoiceAiProvider>>());
            }
            else if (llmOptions.RetryCount > 0)
            {
                resilient = new ResilientInvoiceAiProvider(
                    primary,
                    primaryName,
                    fallbackProvider: null,
                    fallbackName: null,
                    llmOptions.RetryCount,
                    llmOptions.RetryDelayMs,
                    serviceProvider.GetRequiredService<ILogger<ResilientInvoiceAiProvider>>());
            }

            return WrapProvider(serviceProvider, resilient);
        });

        return services;
    }

    private static void ValidateProviderName(string providerName)
    {
        if (string.Equals(providerName, LlmProviderNames.Stub, StringComparison.OrdinalIgnoreCase)
            || string.Equals(providerName, LlmProviderNames.OpenRouter, StringComparison.OrdinalIgnoreCase)
            || string.Equals(providerName, LlmProviderNames.Gemini, StringComparison.OrdinalIgnoreCase)
            || string.Equals(providerName, LlmProviderNames.Groq, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Unsupported {LlmProviderOptions.SectionName} provider value '{providerName}'. " +
            $"Supported values: {LlmProviderNames.Stub}, {LlmProviderNames.OpenRouter}, " +
            $"{LlmProviderNames.Gemini}, {LlmProviderNames.Groq}.");
    }

    private static void RegisterHttpClient(IServiceCollection services, string providerName, bool isFallback)
    {
        if (string.Equals(providerName, LlmProviderNames.Stub, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var clientName = ResolveHttpClientName(providerName, isFallback);
        services.AddHttpClient(clientName, (serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<LlmProviderOptions>>().Value;
            var baseUrl = ResolveBaseUrl(providerName, options, isFallback);
            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 120));
        });
    }

    private static IInvoiceAiProvider CreateProviderInstance(
        IServiceProvider serviceProvider,
        string providerName,
        bool isFallback,
        LlmProviderOptions options)
    {
        if (string.Equals(providerName, LlmProviderNames.Stub, StringComparison.OrdinalIgnoreCase))
        {
            return serviceProvider.GetRequiredService<StubInvoiceAiProvider>();
        }

        var httpClient = serviceProvider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(ResolveHttpClientName(providerName, isFallback));
        var keyAccessor = serviceProvider.GetRequiredService<LlmApiKeyAccessor>();
        var keyedAccessor = isFallback ? keyAccessor.FallbackView : keyAccessor.PrimaryView;
        var providerOptions = Options.Create(new LlmProviderOptions
        {
            Provider = providerName,
            Model = ResolveModel(providerName, options, isFallback),
            BaseUrl = ResolveBaseUrl(providerName, options, isFallback),
            TimeoutSeconds = options.TimeoutSeconds,
            RetryCount = options.RetryCount,
            RetryDelayMs = options.RetryDelayMs
        });

        if (string.Equals(providerName, LlmProviderNames.OpenRouter, StringComparison.OrdinalIgnoreCase))
        {
            return new OpenRouterInvoiceAiProvider(
                httpClient,
                serviceProvider.GetRequiredService<IPolicyRetriever>(),
                keyedAccessor,
                providerOptions,
                serviceProvider.GetRequiredService<ILogger<OpenRouterInvoiceAiProvider>>());
        }

        if (string.Equals(providerName, LlmProviderNames.Gemini, StringComparison.OrdinalIgnoreCase))
        {
            return new GeminiInvoiceAiProvider(
                httpClient,
                serviceProvider.GetRequiredService<IPolicyRetriever>(),
                keyedAccessor,
                providerOptions,
                serviceProvider.GetRequiredService<ILogger<GeminiInvoiceAiProvider>>());
        }

        if (string.Equals(providerName, LlmProviderNames.Groq, StringComparison.OrdinalIgnoreCase))
        {
            return new GroqInvoiceAiProvider(
                httpClient,
                serviceProvider.GetRequiredService<IPolicyRetriever>(),
                keyedAccessor,
                providerOptions,
                serviceProvider.GetRequiredService<ILogger<GroqInvoiceAiProvider>>());
        }

        throw new InvalidOperationException($"Unsupported provider '{providerName}'.");
    }

    private static string ResolveHttpClientName(string providerName, bool isFallback)
    {
        if (string.Equals(providerName, LlmProviderNames.OpenRouter, StringComparison.OrdinalIgnoreCase))
        {
            return isFallback ? OpenRouterFallbackHttpClientName : OpenRouterHttpClientName;
        }

        if (string.Equals(providerName, LlmProviderNames.Gemini, StringComparison.OrdinalIgnoreCase))
        {
            return isFallback ? GeminiFallbackHttpClientName : GeminiHttpClientName;
        }

        if (string.Equals(providerName, LlmProviderNames.Groq, StringComparison.OrdinalIgnoreCase))
        {
            return isFallback ? GroqFallbackHttpClientName : GroqHttpClientName;
        }

        throw new InvalidOperationException($"No HTTP client mapping for provider '{providerName}'.");
    }

    private static string ResolveModel(string providerName, LlmProviderOptions options, bool isFallback)
    {
        if (isFallback)
        {
            return string.IsNullOrWhiteSpace(options.FallbackModel)
                ? LlmProviderNames.DefaultModel(providerName)
                : options.FallbackModel.Trim();
        }

        return string.IsNullOrWhiteSpace(options.Model)
            ? LlmProviderNames.DefaultModel(providerName)
            : options.Model.Trim();
    }

    private static string ResolveBaseUrl(string providerName, LlmProviderOptions options, bool isFallback)
    {
        if (isFallback)
        {
            return NormalizeBaseUrl(
                string.IsNullOrWhiteSpace(options.FallbackBaseUrl)
                    ? LlmProviderNames.DefaultBaseUrl(providerName)
                    : options.FallbackBaseUrl);
        }

        return NormalizeBaseUrl(
            string.IsNullOrWhiteSpace(options.BaseUrl)
                ? LlmProviderNames.DefaultBaseUrl(providerName)
                : options.BaseUrl);
    }

    private static string NormalizeBaseUrl(string baseUrl) => baseUrl.TrimEnd('/') + "/";

    private static IInvoiceAiProvider WrapProvider(IServiceProvider serviceProvider, IInvoiceAiProvider innerProvider)
    {
        var rateLimitOptions = serviceProvider.GetRequiredService<IOptions<RateLimitOptions>>().Value;
        var rateLimitedGate = new RateLimitedGate(
            rateLimitOptions.PermitLimit,
            TimeSpan.FromSeconds(rateLimitOptions.WindowSeconds));
        var throttledProvider = new ThrottledInvoiceAiProvider(innerProvider, rateLimitedGate);
        var metrics = serviceProvider.GetRequiredService<ObservabilityMetrics>();
        return new TracedInvoiceAiProvider(throttledProvider, metrics);
    }
}
