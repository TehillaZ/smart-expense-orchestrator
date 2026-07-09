namespace ApprovalFlow.PolicyRag;

public interface IPolicyRetriever
{
    Task<PolicyRetrievalResult> RetrieveAsync(
        string category,
        decimal amount,
        CancellationToken cancellationToken = default);
}
