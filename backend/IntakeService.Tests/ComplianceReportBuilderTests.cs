using IntakeService.Models;
using IntakeService.Services;

namespace IntakeService.Tests;

public class ComplianceReportBuilderTests
{
    [Fact]
    public void Build_UsesAutonomyCeilingAtDecisionForComplianceCheck()
    {
        var submissions = new[]
        {
            new SubmissionStatusResponse(
                "tracking-4",
                "correlation-4",
                "Delta Co",
                "INV-1004",
                500m,
                "Travel",
                WorkflowStatuses.AutoApproved,
                "Auto-approved within policy.",
                AutonomyCeilingAtDecision: 600m),
        };

        var report = ComplianceReportBuilder.Build(submissions, 400m, "v2");

        Assert.Equal(1, report.AutoApprovedReviewedCount);
        Assert.Equal(0, report.ViolationCount);
    }

    [Fact]
    public void Build_FlagsAutoApprovedSubmissionAboveCeilingAsViolation()
    {
        var submissions = new[]
        {
            new SubmissionStatusResponse(
                "tracking-1",
                "correlation-1",
                "Acme Corp",
                "INV-1001",
                500m,
                "Travel",
                WorkflowStatuses.AutoApproved,
                "Auto-approved within policy."),
            new SubmissionStatusResponse(
                "tracking-2",
                "correlation-2",
                "Beta LLC",
                "INV-1002",
                250m,
                "Meals",
                WorkflowStatuses.AutoApproved,
                "Auto-approved within policy."),
        };

        var report = ComplianceReportBuilder.Build(submissions, 400m, "v1");

        Assert.Equal(2, report.AutoApprovedReviewedCount);
        Assert.Equal(1, report.ViolationCount);
        Assert.Contains(report.AutoApprovedItems, item => item.TrackingId == "tracking-1" && item.IsViolation);
        Assert.Contains(report.AutoApprovedItems, item => item.TrackingId == "tracking-2" && !item.IsViolation);
    }

    [Fact]
    public void Build_TreatsPaidWithoutHumanDecisionAsAutoApproved()
    {
        var submissions = new[]
        {
            new SubmissionStatusResponse(
                "tracking-3",
                "correlation-3",
                "Gamma Inc",
                "INV-1003",
                120m,
                "Software",
                WorkflowStatuses.Paid,
                "Payment completed.",
                "Approve",
                0.95m),
        };

        var report = ComplianceReportBuilder.Build(submissions, 400m, "v1");

        Assert.Equal(1, report.AutoApprovedReviewedCount);
        Assert.Equal(0, report.ViolationCount);
    }
}
