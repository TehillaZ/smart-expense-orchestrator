using ApiGateway.Authorization;
using ApprovalFlow.TestSupport;

namespace ApiGateway.Tests;

[Trait(TestLayers.TraitName, TestLayers.Api)]
public class GatewayRouteAccessTests
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/auth/login")]
    public void IsPublicPath_AllowsHealthAndLogin(string path)
    {
        Assert.True(GatewayRouteAccess.IsPublicPath(path));
    }

    [Fact]
    public void IsPublicPath_RejectsProtectedRoutes()
    {
        Assert.False(GatewayRouteAccess.IsPublicPath("/submissions"));
        Assert.False(GatewayRouteAccess.IsPublicPath("/approvals/queue"));
    }

    [Theory]
    [InlineData("Submitter", "POST", "/submissions", true)]
    [InlineData("Submitter", "GET", "/submissions", true)]
    [InlineData("Submitter", "GET", "/submissions/abc-123", true)]
    [InlineData("Approver", "GET", "/approvals/queue", true)]
    [InlineData("Approver", "POST", "/approvals/abc-123/decision", true)]
    [InlineData("Admin", "GET", "/policy", true)]
    [InlineData("Admin", "PUT", "/policy", true)]
    [InlineData("Admin", "GET", "/audit/abc-123", true)]
    [InlineData("Admin", "GET", "/reports/summary", true)]
    public void IsRoleAllowed_PermitsExpectedRoutes(string role, string method, string path, bool expected)
    {
        Assert.Equal(expected, GatewayRouteAccess.IsRoleAllowed(method, path, role));
    }

    [Theory]
    [InlineData("Submitter", "POST", "/approvals/abc-123/decision")]
    [InlineData("Submitter", "GET", "/approvals/queue")]
    [InlineData("Submitter", "PUT", "/policy")]
    [InlineData("Approver", "PUT", "/policy")]
    [InlineData("Approver", "POST", "/submissions")]
    [InlineData("Admin", "POST", "/submissions")]
    [InlineData("Admin", "GET", "/approvals/queue")]
    public void IsRoleAllowed_RejectsCrossRoleAccess(string role, string method, string path)
    {
        Assert.False(GatewayRouteAccess.IsRoleAllowed(method, path, role));
    }
}
