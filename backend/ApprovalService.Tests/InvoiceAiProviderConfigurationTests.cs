using ApprovalFlow.PolicyRag;
using ApprovalService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApprovalService.Tests;

public class InvoiceAiProviderConfigurationTests
{
    [Fact]
    public void AddConfiguredInvoiceAiProvider_WithStubSetting_RegistersStubProvider()
    {
        var configuration = BuildConfiguration("Stub");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConfiguredInvoiceAiProvider(configuration);

        using var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IInvoiceAiProvider>();

        Assert.IsType<TracedInvoiceAiProvider>(resolved);
    }

    [Fact]
    public void AddConfiguredInvoiceAiProvider_WithoutSetting_DefaultsToStubProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PolicyRag:PolicyDocumentPath"] = PolicyDocumentLocator.Resolve("Data/policy.md"),
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConfiguredInvoiceAiProvider(configuration);

        using var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IInvoiceAiProvider>();

        Assert.IsType<TracedInvoiceAiProvider>(resolved);
    }

    [Fact]
    public void AddConfiguredInvoiceAiProvider_WithOpenRouterSetting_RegistersOpenRouterProvider()
    {
        var configuration = BuildConfiguration(LlmProviderNames.OpenRouter);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConfiguredInvoiceAiProvider(configuration);

        using var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IInvoiceAiProvider>();

        Assert.IsType<TracedInvoiceAiProvider>(resolved);
    }

    [Fact]
    public void AddConfiguredInvoiceAiProvider_WithGeminiSetting_RegistersGeminiProvider()
    {
        var configuration = BuildConfiguration(LlmProviderNames.Gemini);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConfiguredInvoiceAiProvider(configuration);

        using var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IInvoiceAiProvider>();

        Assert.IsType<TracedInvoiceAiProvider>(resolved);
    }

    [Fact]
    public void AddConfiguredInvoiceAiProvider_WithGroqSetting_RegistersGroqProvider()
    {
        var configuration = BuildConfiguration(LlmProviderNames.Groq);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConfiguredInvoiceAiProvider(configuration);

        using var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IInvoiceAiProvider>();

        Assert.IsType<TracedInvoiceAiProvider>(resolved);
    }

    [Fact]
    public void AddConfiguredInvoiceAiProvider_WithFallbackProvider_RegistersSuccessfully()
    {
        var policyPath = PolicyDocumentLocator.Resolve("Data/policy.md");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{LlmProviderOptions.SectionName}:Provider"] = LlmProviderNames.Gemini,
                [$"{LlmProviderOptions.SectionName}:FallbackProvider"] = LlmProviderNames.Groq,
                [$"{LlmProviderOptions.SectionName}:RetryCount"] = "1",
                ["PolicyRag:PolicyDocumentPath"] = policyPath,
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddConfiguredInvoiceAiProvider(configuration);

        using var provider = services.BuildServiceProvider();
        var resolved = provider.GetRequiredService<IInvoiceAiProvider>();

        Assert.IsType<TracedInvoiceAiProvider>(resolved);
    }

    [Fact]
    public void AddConfiguredInvoiceAiProvider_WithUnknownProvider_Throws()
    {
        var configuration = BuildConfiguration("UnknownProvider");
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddConfiguredInvoiceAiProvider(configuration));

        Assert.Contains("UnknownProvider", exception.Message, StringComparison.Ordinal);
        Assert.Contains(LlmProviderNames.Groq, exception.Message, StringComparison.Ordinal);
    }

    private static IConfiguration BuildConfiguration(string providerName)
    {
        var policyPath = PolicyDocumentLocator.Resolve("Data/policy.md");
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{LlmProviderOptions.SectionName}:Provider"] = providerName,
                ["PolicyRag:PolicyDocumentPath"] = policyPath,
            })
            .Build();
    }
}
