using ApprovalFlow.Observability;
using ApprovalService.Services;
using Microsoft.AspNetCore.Http;

namespace ApprovalService.Tests;

public class ApproverIdentityResolverTests
{
    [Fact]
    public void Resolve_WithHeader_ReturnsTrimmedUsername()
    {
        var headers = new HeaderDictionary
        {
            [UserIdentityConstants.HeaderName] = "  Tehila Cohen  ",
        };

        var username = ApproverIdentityResolver.Resolve(headers);

        Assert.Equal("Tehila Cohen", username);
    }

    [Fact]
    public void Resolve_WithoutHeader_UsesDecidedByFallback()
    {
        var headers = new HeaderDictionary();

        var username = ApproverIdentityResolver.Resolve(headers, "  Dana Levi  ");

        Assert.Equal("Dana Levi", username);
    }

    [Fact]
    public void Resolve_PrefersHeaderOverDecidedBy()
    {
        var headers = new HeaderDictionary
        {
            [UserIdentityConstants.HeaderName] = "Header User",
        };

        var username = ApproverIdentityResolver.Resolve(headers, "Body User");

        Assert.Equal("Header User", username);
    }

    [Fact]
    public void Resolve_WithoutHeaderOrBody_ReturnsNull()
    {
        var headers = new HeaderDictionary();

        var username = ApproverIdentityResolver.Resolve(headers);

        Assert.Null(username);
    }
}
