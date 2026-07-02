using ApprovalService.Routes;
using ApprovalService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();
builder.Services.AddSingleton<ISubmissionStateStore, DaprSubmissionStateStore>();
builder.Services.AddSingleton<SubmissionReceivedHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCloudEvents();

app.MapApprovalRoutes();
app.MapHealthRoutes();
app.MapSubscriptionRoutes();
app.MapSubscribeHandler();

app.Run();
