using IntakeService.Models;

namespace IntakeService.Services;

public static class ComplianceReportBuilder
{
    public static ComplianceReportResponse Build(
        IReadOnlyList<SubmissionStatusResponse> submissions,
        decimal autonomyCeiling,
        string policyVersion)
    {
        var autoApprovedItems = submissions
            .Where(WasAutoApproved)
            .Select(submission => ToComplianceItem(submission, autonomyCeiling))
            .OrderByDescending(item => item.IsViolation)
            .ThenByDescending(item => item.Amount)
            .ToArray();

        return new ComplianceReportResponse(
            autonomyCeiling,
            policyVersion,
            autoApprovedItems.Length,
            autoApprovedItems.Count(item => item.IsViolation),
            autoApprovedItems);
    }

    private static AutoApprovalComplianceItem ToComplianceItem(
        SubmissionStatusResponse submission,
        decimal autonomyCeiling)
    {
        var withinCeiling = submission.Amount <= autonomyCeiling;

        return new AutoApprovalComplianceItem(
            submission.TrackingId,
            submission.InvoiceNumber,
            submission.Vendor,
            submission.Amount,
            withinCeiling,
            !withinCeiling);
    }

    private static bool WasAutoApproved(SubmissionStatusResponse submission)
    {
        if (string.Equals(submission.Status, WorkflowStatuses.AutoApproved, StringComparison.Ordinal))
        {
            return true;
        }

        return string.Equals(submission.Status, WorkflowStatuses.Paid, StringComparison.Ordinal)
            && string.IsNullOrWhiteSpace(submission.HumanDecision)
            && !string.Equals(submission.AiRecommendation, "Escalate", StringComparison.OrdinalIgnoreCase);
    }
}
