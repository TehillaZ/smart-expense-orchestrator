using ApprovalService.Models;
using ApprovalService.Services;

namespace ApprovalService.Tests;

public class PolicySettingsValidatorTests
{
    [Fact]
    public void Validate_WithValidUpdate_ReturnsNoErrors()
    {
        var request = new PolicySettingsUpdateRequest(250m, 0.85m);

        var errors = PolicySettingsValidator.Validate(request);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Validate_WithInvalidCeiling_ReturnsError(decimal ceiling)
    {
        var request = new PolicySettingsUpdateRequest(ceiling, null);

        var errors = PolicySettingsValidator.Validate(request);

        Assert.Contains("autonomyCeiling must be greater than 0.", errors);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Validate_WithInvalidConfidence_ReturnsError(decimal confidence)
    {
        var request = new PolicySettingsUpdateRequest(null, confidence);

        var errors = PolicySettingsValidator.Validate(request);

        Assert.Contains("autonomyConfidence must be between 0 and 1.", errors);
    }
}
