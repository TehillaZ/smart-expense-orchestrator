using ApprovalFlow.PolicyRag;
using ApprovalService.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ApprovalService.Tests;

internal static class PolicyRagTestHelper
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

    public static StubInvoiceAiProvider CreateStubProvider() =>
        new(CreateRetriever());
}
