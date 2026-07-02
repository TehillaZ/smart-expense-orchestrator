using ApprovalService.Models;
using ApprovalService.Services;

namespace ApprovalService.Tests;

public class ApprovalDecisionValidatorTests
{
    [Theory]
    [InlineData("Approve")]
    [InlineData("Reject")]
    [InlineData("RequestInfo")]
    [InlineData("approve")]
    public void Validate_WithAllowedDecision_ReturnsNoErrors(string decision)
    {
        var request = new ApprovalDecisionRequest(decision, "Looks fine", "correlation-1");

        var errors = ApprovalDecisionValidator.Validate(request);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingDecision_ReturnsError(string? decision)
    {
        var request = new ApprovalDecisionRequest(decision, null, null);

        var errors = ApprovalDecisionValidator.Validate(request);

        Assert.Contains("decision is required.", errors);
    }

    [Fact]
    public void Validate_WithUnsupportedDecision_ReturnsError()
    {
        var request = new ApprovalDecisionRequest("ApproveAnyway", null, null);

        var errors = ApprovalDecisionValidator.Validate(request);

        Assert.Contains("decision must be one of: Approve, Reject, RequestInfo.", errors);
    }
}
