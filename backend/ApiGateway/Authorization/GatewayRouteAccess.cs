using ApiGateway.Auth;

namespace ApiGateway.Authorization;

public static class GatewayRouteAccess
{
    public static bool IsPublicPath(PathString path)
    {
        if (!path.HasValue)
        {
            return false;
        }

        var normalized = path.Value!.TrimEnd('/');
        return normalized.Equals("/health", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("/auth/login", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("/openapi/v1.json", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("/scalar", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("/scalar/", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsRoleAllowed(string method, PathString path, string? role)
    {
        if (!AppRoles.IsValid(role))
        {
            return false;
        }

        if (!path.HasValue)
        {
            return false;
        }

        var normalizedPath = path.Value!.TrimEnd('/');
        var normalizedMethod = method.ToUpperInvariant();

        if (normalizedPath.Equals("/submissions", StringComparison.OrdinalIgnoreCase)
            && (normalizedMethod is "GET" or "POST"))
        {
            return role == AppRoles.Submitter;
        }

        if (normalizedPath.StartsWith("/submissions/", StringComparison.OrdinalIgnoreCase)
            && normalizedMethod == "GET")
        {
            return role == AppRoles.Submitter;
        }

        if (normalizedPath.StartsWith("/notifications/", StringComparison.OrdinalIgnoreCase)
            && normalizedMethod == "GET")
        {
            return role == AppRoles.Submitter;
        }

        if (normalizedPath.Equals("/approvals/queue", StringComparison.OrdinalIgnoreCase)
            && normalizedMethod == "GET")
        {
            return role == AppRoles.Approver;
        }

        if (normalizedPath.StartsWith("/approvals/", StringComparison.OrdinalIgnoreCase))
        {
            return role == AppRoles.Approver;
        }

        if (normalizedPath.Equals("/policy", StringComparison.OrdinalIgnoreCase)
            && normalizedMethod is "GET" or "PUT")
        {
            return role == AppRoles.Admin;
        }

        if (normalizedPath.StartsWith("/audit/", StringComparison.OrdinalIgnoreCase)
            && normalizedMethod == "GET")
        {
            return role == AppRoles.Admin;
        }

        if (normalizedPath.StartsWith("/reports/", StringComparison.OrdinalIgnoreCase)
            && normalizedMethod == "GET")
        {
            return role == AppRoles.Admin;
        }

        return false;
    }
}
