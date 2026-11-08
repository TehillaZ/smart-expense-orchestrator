using ApprovalFlow.Observability;

namespace ApprovalService.Services;

public static class ApproverIdentityResolver
{
    public const string InvalidApproverNameError =
        "approver name must not contain line breaks or other control characters.";

    /// <summary>
    /// Resolves the approver name like <see cref="Resolve"/>, but refuses names that
    /// contain line breaks or other control characters. Such a name can only come from a
    /// crafted request, and it could forge extra log lines or pollute the audit trail.
    /// </summary>
    /// <returns>false when the name must be rejected; <paramref name="approver"/> is then null.</returns>
    public static bool TryResolve(IHeaderDictionary headers, string? decidedBy, out string? approver)
    {
        approver = Resolve(headers, decidedBy);
        if (approver is not null && approver.Any(char.IsControl))
        {
            approver = null;
            return false;
        }

        return true;
    }

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
