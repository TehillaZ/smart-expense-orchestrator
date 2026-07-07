namespace ApprovalService.Services;

public static class InvoiceAiProviderRegistration
{
    public static IServiceCollection AddConfiguredInvoiceAiProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<LlmProviderOptions>(
            configuration.GetSection(LlmProviderOptions.SectionName));

        var providerName = configuration
            .GetSection(LlmProviderOptions.SectionName)
            .GetValue<string>(nameof(LlmProviderOptions.Provider)) ?? LlmProviderNames.Stub;

        switch (providerName.Trim().ToUpperInvariant())
        {
            case var name when string.Equals(name, LlmProviderNames.Stub, StringComparison.OrdinalIgnoreCase):
                services.AddSingleton<IInvoiceAiProvider, StubInvoiceAiProvider>();
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported {LlmProviderOptions.SectionName}:Provider value '{providerName}'. " +
                    $"Supported values: {LlmProviderNames.Stub}.");
        }

        return services;
    }
}
