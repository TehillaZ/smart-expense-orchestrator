using NotificationService.Routes;
using NotificationService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();
builder.Services.AddSingleton<INotificationStore, DaprNotificationStore>();
builder.Services.AddSingleton<INotificationChannel, StubNotificationChannel>();
builder.Services.AddSingleton<NotificationProcessor>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCloudEvents();

app.MapHealthRoutes();
app.MapNotificationRoutes();
app.MapSubscriptionRoutes();
app.MapSubscribeHandler();

app.Run();
