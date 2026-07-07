using ApprovalService.Services;

namespace ApprovalService.Tests;

public class PolicyVersionHelperTests
{
    [Theory]
    [InlineData("v1", "v2")]
    [InlineData("v9", "v10")]
    public void Bump_IncrementsNumericVersion(string current, string expected)
    {
        Assert.Equal(expected, PolicyVersionHelper.Bump(current));
    }

    [Fact]
    public void Bump_FallsBackToV2ForUnknownFormat()
    {
        Assert.Equal("v2", PolicyVersionHelper.Bump("2026-07-07"));
    }
}
