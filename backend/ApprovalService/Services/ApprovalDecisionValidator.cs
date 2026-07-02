using ApprovalService.Models;

namespace ApprovalService.Services;

public static class ApprovalDecisionValidator
{
    private static readonly HashSet<string> AllowedDecisions = new(StringComparer.OrdinalIgnoreCase)
    {
        "Approve",
        "Reject",
        "RequestInfo"
    };

    public static IReadOnlyList<string> Validate(ApprovalDecisionRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Decision))
        {
            errors.Add("decision is required.");
        }
        else if (!AllowedDecisions.Contains(request.Decision.Trim()))
        {
            errors.Add("decision must be one of: Approve, Reject, RequestInfo.");
        }

        return errors;
    }
}
