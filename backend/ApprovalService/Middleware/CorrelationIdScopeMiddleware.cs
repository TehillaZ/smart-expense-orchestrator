using ApprovalFlow.Observability;
using Microsoft.Extensions.Logging;

namespace ApprovalService.Middleware;

public sealed class CorrelationIdScopeMiddleware
{
    public const string HeaderName = CorrelationConstants.HeaderName;

    private readonly RequestDelegate next;
    private readonly ILogger<CorrelationIdScopeMiddleware> logger;

    public CorrelationIdScopeMiddleware(RequestDelegate next, ILogger<CorrelationIdScopeMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = CorrelationIdResolver.Resolve(context.Request.Headers[HeaderName].FirstOrDefault());

        context.Request.Headers[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (TraceLogScope.Begin(logger, correlationId))
        {
            await next(context);
        }
    }
}
