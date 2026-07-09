using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ApprovalFlow.PolicyRag;

public static class PolicyRagServiceCollectionExtensions
{
    public static IServiceCollection AddPolicyRag(
        this IServiceCollection services,
        Action<PolicyRagOptions>? configure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<PolicyRagOptions>(_ => { });
        }

        services.TryAddSingleton<InMemoryPolicyVectorStore>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PolicyRagOptions>>().Value;
            var policyPath = PolicyDocumentLocator.Resolve(options.PolicyDocumentPath);
            var clauses = PolicyDocumentChunker.ChunkFromFile(policyPath);
            return new InMemoryPolicyVectorStore(clauses);
        });
        services.TryAddSingleton<IPolicyRetriever, PolicyRetriever>();

        return services;
    }
}
