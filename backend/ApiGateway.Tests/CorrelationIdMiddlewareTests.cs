using ApiGateway.Middleware;
using Microsoft.AspNetCore.Http;

namespace ApiGateway.Tests;

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithoutHeader_GeneratesCorrelationId()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        string? observedCorrelationId = null;

        var middleware = new CorrelationIdMiddleware(next: ctx =>
        {
            observedCorrelationId = ctx.Request.Headers[CorrelationIdMiddleware.HeaderName].FirstOrDefault();
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.False(string.IsNullOrWhiteSpace(observedCorrelationId));
        Assert.Equal(observedCorrelationId, context.Response.Headers[CorrelationIdMiddleware.HeaderName].FirstOrDefault());
    }

    [Fact]
    public async Task InvokeAsync_WithExistingHeader_PreservesCorrelationId()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "existing-correlation-id";
        string? observedCorrelationId = null;

        var middleware = new CorrelationIdMiddleware(next: ctx =>
        {
            observedCorrelationId = ctx.Request.Headers[CorrelationIdMiddleware.HeaderName].FirstOrDefault();
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.Equal("existing-correlation-id", observedCorrelationId);
        Assert.Equal("existing-correlation-id", context.Response.Headers[CorrelationIdMiddleware.HeaderName].FirstOrDefault());
    }
}
