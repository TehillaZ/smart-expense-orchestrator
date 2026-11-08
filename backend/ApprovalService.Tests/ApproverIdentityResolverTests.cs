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

    [Fact]
    public void TryResolve_WithNewLineInName_RejectsSoNoFakeLogLineCanBeCreated()
    {
        var headers = new HeaderDictionary();

        var accepted = ApproverIdentityResolver.TryResolve(headers, "Tehila\nadmin approved", out var username);

        Assert.False(accepted);
        Assert.Null(username);
    }

    [Fact]
    public void TryResolve_WithCarriageReturnInName_Rejects()
    {
        var headers = new HeaderDictionary();

        var accepted = ApproverIdentityResolver.TryResolve(headers, "Tehila\radmin approved", out var username);

        Assert.False(accepted);
        Assert.Null(username);
    }

    [Fact]
    public void Normalize_WithCleanNameAndTrailingSpaces_KeepsNameAndTrimsSpaces()
    {
        var username = ApproverIdentityResolver.Normalize("Tehila admin approved  ");

        Assert.Equal("Tehila admin approved", username);
    }

    [Fact]
    public void Normalize_WithNull_ReturnsNull()
    {
        var username = ApproverIdentityResolver.Normalize(null);

        Assert.Null(username);
    }
}
