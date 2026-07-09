using ApprovalFlow.PolicyRag;
using ApprovalService.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ApprovalFlow.JourneyTests;

internal static class JourneyPolicyRagHelper
{
    public static StubInvoiceAiProvider CreateStubProvider()
    {
        var path = PolicyDocumentLocator.Resolve("Data/policy.md");
        var clauses = PolicyDocumentChunker.ChunkFromFile(path);
        var vectorStore = new InMemoryPolicyVectorStore(clauses);
        var retriever = new PolicyRetriever(
            vectorStore,
            Options.Create(new PolicyRagOptions { PolicyDocumentPath = path }),
            NullLogger<PolicyRetriever>.Instance);

        return new StubInvoiceAiProvider(retriever);
    }
}
