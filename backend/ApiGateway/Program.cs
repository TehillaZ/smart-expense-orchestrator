using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using ApiGateway.Middleware;
using ApiGateway.Routes;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("gateway", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 100;
        limiter.QueueLimit = 0;
    });
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Rate limit exceeded.", cancellationToken);
    };
});

var app = builder.Build();

app.UseRateLimiter();
app.UseMiddleware<CorrelationIdMiddleware>();
app.MapHealthRoutes();
app.MapReverseProxy().RequireRateLimiting("gateway");

app.Run();
