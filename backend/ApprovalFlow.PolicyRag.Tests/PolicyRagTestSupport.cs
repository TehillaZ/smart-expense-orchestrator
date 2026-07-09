using ApprovalFlow.PolicyRag;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ApprovalFlow.PolicyRag.Tests;

public static class PolicyRagTestSupport
{
    public static IPolicyRetriever CreateRetriever()
    {
        var path = PolicyDocumentLocator.Resolve("Data/policy.md");
        var clauses = PolicyDocumentChunker.ChunkFromFile(path);
        var vectorStore = new InMemoryPolicyVectorStore(clauses);
        return new PolicyRetriever(
            vectorStore,
            Options.Create(new PolicyRagOptions { PolicyDocumentPath = path }),
            NullLogger<PolicyRetriever>.Instance);
    }
}
