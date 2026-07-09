using ApprovalFlow.Observability;
using IntakeService.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace IntakeService.Tests;

public class CorrelationIdScopeMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithoutHeader_GeneratesAndWritesRequestHeader()
    {
        var context = new DefaultHttpContext();
        string? observed = null;

        var middleware = new CorrelationIdScopeMiddleware(
            next: ctx =>
            {
                observed = ctx.Request.Headers[CorrelationIdScopeMiddleware.HeaderName].FirstOrDefault();
                return Task.CompletedTask;
            },
            logger: NullLogger<CorrelationIdScopeMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.False(string.IsNullOrWhiteSpace(observed));
        Assert.Equal(observed, context.Response.Headers[CorrelationIdScopeMiddleware.HeaderName].FirstOrDefault());
    }

    [Fact]
    public async Task InvokeAsync_WithExistingHeader_PreservesCorrelationId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdScopeMiddleware.HeaderName] = "gateway-correlation";
        string? observed = null;

        var middleware = new CorrelationIdScopeMiddleware(
            next: ctx =>
            {
                observed = ctx.Request.Headers[CorrelationIdScopeMiddleware.HeaderName].FirstOrDefault();
                return Task.CompletedTask;
            },
            logger: NullLogger<CorrelationIdScopeMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Equal("gateway-correlation", observed);
        Assert.Equal("gateway-correlation", context.Response.Headers[CorrelationIdScopeMiddleware.HeaderName].FirstOrDefault());
    }
}
