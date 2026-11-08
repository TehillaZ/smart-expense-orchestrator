using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ApiGateway.Auth;

public static class GatewayUserIdentityResolver
{
    public static string? ResolveUsername(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        foreach (var candidate in new[]
                 {
                     user.FindFirstValue(ClaimTypes.Name),
                     user.FindFirstValue("unique_name"),
                     user.FindFirstValue(JwtRegisteredClaimNames.Sub),
                     user.FindFirstValue(ClaimTypes.NameIdentifier),
                 })
        {
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate.Trim();
            }
        }

        return null;
    }
}
