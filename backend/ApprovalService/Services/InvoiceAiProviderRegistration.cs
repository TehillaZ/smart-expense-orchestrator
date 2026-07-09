using ApprovalFlow.Observability;
using ApprovalFlow.PolicyRag;
using ApprovalFlow.Reliability.Throttling;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ApprovalService.Services;

public static class InvoiceAiProviderRegistration
{
    public const string OpenRouterHttpClientName = "openrouter-invoice-ai";

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

        var providerName = configuration
            .GetSection(LlmProviderOptions.SectionName)
            .GetValue<string>(nameof(LlmProviderOptions.Provider)) ?? LlmProviderNames.Stub;

        switch (providerName.Trim().ToUpperInvariant())
        {
            case var name when string.Equals(name, LlmProviderNames.Stub, StringComparison.OrdinalIgnoreCase):
                services.AddSingleton<StubInvoiceAiProvider>();
                services.AddSingleton<IInvoiceAiProvider>(serviceProvider =>
                    WrapProvider(serviceProvider, serviceProvider.GetRequiredService<StubInvoiceAiProvider>()));
                break;

            case var name when string.Equals(name, LlmProviderNames.OpenRouter, StringComparison.OrdinalIgnoreCase):
                services.AddHttpClient(OpenRouterHttpClientName, (serviceProvider, client) =>
                {
                    var options = serviceProvider.GetRequiredService<IOptions<LlmProviderOptions>>().Value;
                    var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
                        ? "https://openrouter.ai/api/v1/"
                        : options.BaseUrl.TrimEnd('/') + "/";
                    client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
                    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 5, 120));
                });
                services.AddSingleton<IInvoiceAiProvider>(serviceProvider =>
                {
                    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                    var httpClient = httpClientFactory.CreateClient(OpenRouterHttpClientName);
                    var innerProvider = new OpenRouterInvoiceAiProvider(
                        httpClient,
                        serviceProvider.GetRequiredService<IPolicyRetriever>(),
                        serviceProvider.GetRequiredService<LlmApiKeyAccessor>(),
                        serviceProvider.GetRequiredService<IOptions<LlmProviderOptions>>(),
                        serviceProvider.GetRequiredService<ILogger<OpenRouterInvoiceAiProvider>>());
                    return WrapProvider(serviceProvider, innerProvider);
                });
                break;

            default:
                throw new InvalidOperationException(
                    $"Unsupported {LlmProviderOptions.SectionName}:Provider value '{providerName}'. " +
                    $"Supported values: {LlmProviderNames.Stub}, {LlmProviderNames.OpenRouter}.");
        }

        return services;
    }

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
