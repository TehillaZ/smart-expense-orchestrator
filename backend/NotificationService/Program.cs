using ApprovalFlow.Observability;
using Microsoft.OpenApi.Models;
using NotificationService.Middleware;
using NotificationService.Routes;
using NotificationService.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddApprovalFlowObservability("NotificationService");

builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);

builder.Services.AddDaprClient();
builder.Services.AddSingleton<INotificationStore, DaprNotificationStore>();
builder.Services.AddSingleton<NotificationChannelOptions>();
builder.Services.AddSingleton<DaprSecretsProvider>();
builder.Services.AddHostedService<DaprSecretsBootstrapper>();
builder.Services.AddSingleton<INotificationChannel, StubNotificationChannel>();
builder.Services.AddSingleton<NotificationProcessor>();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "Notification Service API",
            Version = "v1",
            Description = "Notification outcomes for completed or failed payments.",
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Notification Service API")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.UseApprovalFlowObservability();
app.UseMiddleware<CorrelationIdScopeMiddleware>();
app.UseCloudEvents();

app.MapHealthRoutes();
app.MapNotificationRoutes();
app.MapInternalNotificationRoutes();
app.MapSubscriptionRoutes();
app.MapSubscribeHandler();

app.Run();
