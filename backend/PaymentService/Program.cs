using PaymentService.Routes;
using PaymentService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<PaymentSimulationOptions>(
    builder.Configuration.GetSection(PaymentSimulationOptions.SectionName));
builder.Services.AddDaprClient();
builder.Services.AddSingleton<IPaymentSagaStore, DaprPaymentSagaStore>();
builder.Services.AddSingleton<ISubmissionWorkflowStore, DaprSubmissionWorkflowStore>();
builder.Services.AddSingleton<IPaymentGateway, StubPaymentGateway>();
builder.Services.AddSingleton<IPaymentOutcomePublisher, DaprPaymentOutcomePublisher>();
builder.Services.AddSingleton<PaymentSagaProcessor>();
builder.Services.AddSingleton<PaymentRequestedHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCloudEvents();

app.MapHealthRoutes();
app.MapSubscriptionRoutes();
app.MapSubscribeHandler();

app.Run();
