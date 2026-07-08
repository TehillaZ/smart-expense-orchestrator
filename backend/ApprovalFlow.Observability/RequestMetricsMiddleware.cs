using Microsoft.AspNetCore.Http;
namespace ApprovalFlow.Observability;

public sealed class RequestMetricsMiddleware
{
    private readonly RequestDelegate next;
    private readonly ObservabilityMetrics metrics;

    public RequestMetricsMiddleware(RequestDelegate next, ObservabilityMetrics metrics)
    {
        this.next = next;
        this.metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var operation = $"{context.Request.Method} {context.Request.Path}";
        var succeeded = true;

        try
        {
            await next(context);
            succeeded = context.Response.StatusCode < 500;
        }
        catch
        {
            succeeded = false;
            throw;
        }
        finally
        {
            metrics.RecordRequest(operation, succeeded);
        }
    }
}
