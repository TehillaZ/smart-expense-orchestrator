using ApprovalFlow.Observability;
using Microsoft.Extensions.Logging;

namespace NotificationService.Middleware;

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
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
        }

        context.Response.Headers[HeaderName] = correlationId;

        using (TraceLogScope.Begin(logger, correlationId))
        {
            await next(context);
        }
    }
}
