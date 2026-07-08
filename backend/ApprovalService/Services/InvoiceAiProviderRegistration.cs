using ApprovalFlow.Reliability.Throttling;
using Microsoft.Extensions.Options;

namespace ApprovalService.Services;

public static class InvoiceAiProviderRegistration
{
    public static IServiceCollection AddConfiguredInvoiceAiProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<LlmProviderOptions>(
            configuration.GetSection(LlmProviderOptions.SectionName));
        services.Configure<RateLimitOptions>(
            configuration.GetSection(RateLimitOptions.SectionName));

        var providerName = configuration
            .GetSection(LlmProviderOptions.SectionName)
            .GetValue<string>(nameof(LlmProviderOptions.Provider)) ?? LlmProviderNames.Stub;

        switch (providerName.Trim().ToUpperInvariant())
        {
            case var name when string.Equals(name, LlmProviderNames.Stub, StringComparison.OrdinalIgnoreCase):
                services.AddSingleton<StubInvoiceAiProvider>();
                services.AddSingleton<IInvoiceAiProvider>(serviceProvider =>
                {
                    var innerProvider = serviceProvider.GetRequiredService<StubInvoiceAiProvider>();
                    var rateLimitOptions = serviceProvider.GetRequiredService<IOptions<RateLimitOptions>>().Value;
                    var rateLimitedGate = new RateLimitedGate(
                        rateLimitOptions.PermitLimit,
                        TimeSpan.FromSeconds(rateLimitOptions.WindowSeconds));
                    return new ThrottledInvoiceAiProvider(innerProvider, rateLimitedGate);
                });
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported {LlmProviderOptions.SectionName}:Provider value '{providerName}'. " +
                    $"Supported values: {LlmProviderNames.Stub}.");
        }

        return services;
    }
}
