using ApprovalFlow.Observability;

namespace ApprovalService.Services;

public static class ApproverIdentityResolver
{
    public static string? Resolve(IHeaderDictionary headers, string? decidedBy = null)
    {
        var fromHeader = ResolveFromHeaders(headers);
        if (!string.IsNullOrWhiteSpace(fromHeader))
        {
            return fromHeader;
        }

        return Normalize(decidedBy);
    }

    public static string? ResolveFromHeaders(IHeaderDictionary headers)
    {
        if (!headers.TryGetValue(UserIdentityConstants.HeaderName, out var values))
        {
            return null;
        }

        return Normalize(values.FirstOrDefault());
    }

    public static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
