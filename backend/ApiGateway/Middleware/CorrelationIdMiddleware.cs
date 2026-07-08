using ApprovalFlow.Observability;
using Microsoft.Extensions.Logging;

namespace ApiGateway.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = CorrelationConstants.HeaderName;

    private readonly RequestDelegate next;
    private readonly ILogger<CorrelationIdMiddleware> logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
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

        context.Items[HeaderName] = correlationId;
        context.Request.Headers[HeaderName] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (TraceLogScope.Begin(logger, correlationId))
        {
            await next(context);
        }
    }
}
