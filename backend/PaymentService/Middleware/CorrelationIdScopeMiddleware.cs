namespace PaymentService.Middleware;

public sealed class CorrelationIdScopeMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

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

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await next(context);
        }
    }
}
