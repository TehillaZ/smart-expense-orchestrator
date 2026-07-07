using IntakeService.Middleware;
using IntakeService.Routes;
using IntakeService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();
builder.Services.AddSingleton<ISubmissionStore, DaprSubmissionStore>();
builder.Services.AddSingleton<IDashboardMetricsStore, DaprDashboardMetricsStore>();
builder.Services.AddSingleton<DaprNotificationLookup>();
builder.Services.AddSingleton<DaprPolicyLookup>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<CorrelationIdScopeMiddleware>();

app.MapSubmissionRoutes();
app.MapAuditRoutes();
app.MapReportRoutes();
app.MapInternalMetricsRoutes();
app.MapHealthRoutes();

app.Run();
