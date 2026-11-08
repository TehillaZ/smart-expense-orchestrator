using System.Security.Claims;
using ApiGateway.Auth;
using ApprovalFlow.Observability;

namespace ApiGateway.Middleware;

public sealed class GatewayUserIdentityMiddleware
{
    private readonly RequestDelegate _next;

    public GatewayUserIdentityMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var username = GatewayUserIdentityResolver.ResolveUsername(context.User);
        if (!string.IsNullOrWhiteSpace(username))
        {
            context.Request.Headers[UserIdentityConstants.HeaderName] = username;
        }

        await _next(context);
    }
}
