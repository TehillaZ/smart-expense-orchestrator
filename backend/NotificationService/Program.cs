using NotificationService.Middleware;
using NotificationService.Routes;
using NotificationService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);

builder.Services.AddDaprClient();
builder.Services.AddSingleton<INotificationStore, DaprNotificationStore>();
builder.Services.AddSingleton<NotificationChannelOptions>();
builder.Services.AddSingleton<DaprSecretsProvider>();
builder.Services.AddHostedService<DaprSecretsBootstrapper>();
builder.Services.AddSingleton<INotificationChannel, StubNotificationChannel>();
builder.Services.AddSingleton<NotificationProcessor>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<CorrelationIdScopeMiddleware>();
app.UseCloudEvents();

app.MapHealthRoutes();
app.MapNotificationRoutes();
app.MapInternalNotificationRoutes();
app.MapSubscriptionRoutes();
app.MapSubscribeHandler();

app.Run();
