using IntakeService.Routes;
using IntakeService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();
builder.Services.AddSingleton<ISubmissionStore, DaprSubmissionStore>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapSubmissionRoutes();
app.MapHealthRoutes();

app.Run();
