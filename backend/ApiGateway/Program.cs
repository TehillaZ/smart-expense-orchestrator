using System.Text;
using System.Threading.RateLimiting;
using ApiGateway.Auth;
using ApiGateway.Middleware;
using ApiGateway.Routes;
using ApprovalFlow.Observability;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

builder.AddApprovalFlowObservability("ApiGateway");

builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<JwtTokenService>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
{
    throw new InvalidOperationException("JWT signing key is not configured.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(transformBuilderContext =>
    {
        transformBuilderContext.AddRequestTransform(transformContext =>
        {
            var username = GatewayUserIdentityResolver.ResolveUsername(transformContext.HttpContext.User);
            if (string.IsNullOrWhiteSpace(username))
            {
                return ValueTask.CompletedTask;
            }

            transformContext.ProxyRequest.Headers.Remove(UserIdentityConstants.HeaderName);
            transformContext.ProxyRequest.Headers.TryAddWithoutValidation(
                UserIdentityConstants.HeaderName,
                username);
            return ValueTask.CompletedTask;
        });
    });

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("gateway", limiter =>
    {
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.PermitLimit = 600;
        limiter.QueueLimit = 50;
    });
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Rate limit exceeded.", cancellationToken);
    };
});

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "ApprovalFlow API Gateway",
            Version = "v1",
            Description =
                "Edge gateway for ApprovalFlow. Prefer these routes for clients. " +
                "Business APIs are proxied to Intake/Approval/Payment/Notification. " +
                "Authenticate via POST /auth/login, then send Authorization: Bearer <token>.",
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("ApprovalFlow API Gateway")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.UseApprovalFlowObservability();
app.UseRateLimiter();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<GatewayUserIdentityMiddleware>();
app.UseMiddleware<GatewayAuthorizationMiddleware>();
app.MapHealthRoutes();
app.MapAuthRoutes();
app.MapReverseProxy().RequireRateLimiting("gateway");

app.Run();

public partial class Program;
