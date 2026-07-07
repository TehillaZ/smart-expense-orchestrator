using System.Security.Claims;
using ApiGateway.Authorization;

namespace ApiGateway.Middleware;

public sealed class GatewayAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayAuthorizationMiddleware> _logger;

    public GatewayAuthorizationMiddleware(
        RequestDelegate next,
        ILogger<GatewayAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (GatewayRouteAccess.IsPublicPath(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            _logger.LogWarning(
                "Rejected unauthenticated request for {Method} {Path}.",
                context.Request.Method,
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Authentication required." });
            return;
        }

        var role = context.User.FindFirstValue(ClaimTypes.Role);
        if (!GatewayRouteAccess.IsRoleAllowed(context.Request.Method, context.Request.Path, role))
        {
            _logger.LogWarning(
                "Rejected forbidden request for role {Role} on {Method} {Path}.",
                role,
                context.Request.Method,
                context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { message = "Access denied for this role." });
            return;
        }

        await _next(context);
    }
}
