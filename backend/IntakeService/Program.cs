using ApprovalFlow.Observability;
using ApprovalFlow.Reliability.Outbox;
using IntakeService.Middleware;
using IntakeService.Routes;
using IntakeService.Services;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddApprovalFlowObservability("IntakeService");

builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);

builder.Services.AddDaprClient();
builder.Services.AddSingleton<ISubmissionStore, DaprSubmissionStore>();
builder.Services.AddSingleton<IDashboardMetricsStore, DaprDashboardMetricsStore>();
builder.Services.AddSingleton<DaprNotificationLookup>();
builder.Services.AddSingleton<DaprPolicyLookup>();
builder.Services.AddSingleton<IOutboxTransport, IntakeOutboxTransport>();
builder.Services.AddDaprOutbox(options =>
{
    options.Scope = "intake";
});
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "Intake Service API",
            Version = "v1",
            Description = "Accepts expense submissions, tracks status, and exposes audit/report endpoints.",
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Intake Service API")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.UseApprovalFlowObservability();
app.UseMiddleware<CorrelationIdScopeMiddleware>();

app.MapSubmissionRoutes();
app.MapAuditRoutes();
app.MapReportRoutes();
app.MapInternalMetricsRoutes();
app.MapHealthRoutes();

app.Run();
