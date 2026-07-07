using IntakeService.Models;

namespace IntakeService.Services;

public static class ComplianceReportBuilder
{
    public static ComplianceReportResponse Build(
        IReadOnlyList<SubmissionStatusResponse> submissions,
        decimal fallbackAutonomyCeiling,
        string currentPolicyVersion)
    {
        var autoApprovedItems = submissions
            .Where(WasAutoApproved)
            .Select(submission => ToComplianceItem(submission, fallbackAutonomyCeiling))
            .OrderByDescending(item => item.IsViolation)
            .ThenByDescending(item => item.Amount)
            .ToArray();

        return new ComplianceReportResponse(
            fallbackAutonomyCeiling,
            currentPolicyVersion,
            autoApprovedItems.Length,
            autoApprovedItems.Count(item => item.IsViolation),
            autoApprovedItems);
    }

    private static AutoApprovalComplianceItem ToComplianceItem(
        SubmissionStatusResponse submission,
        decimal fallbackAutonomyCeiling)
    {
        var ceiling = submission.AutonomyCeilingAtDecision ?? fallbackAutonomyCeiling;
        var withinCeiling = submission.Amount <= ceiling;

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
