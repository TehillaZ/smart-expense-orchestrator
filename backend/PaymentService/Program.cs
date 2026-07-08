using ApprovalFlow.Reliability.Outbox;
using ApprovalFlow.Reliability.Throttling;
using Microsoft.Extensions.Options;
using PaymentService.Middleware;
using PaymentService.Routes;
using PaymentService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);

builder.Services.Configure<PaymentSimulationOptions>(
    builder.Configuration.GetSection(PaymentSimulationOptions.SectionName));
builder.Services.Configure<BulkheadOptions>(
    builder.Configuration.GetSection(BulkheadOptions.SectionName));
builder.Services.AddDaprClient();
builder.Services.AddSingleton<IPaymentSagaStore, DaprPaymentSagaStore>();
builder.Services.AddSingleton<ISubmissionWorkflowStore, DaprSubmissionWorkflowStore>();
builder.Services.AddSingleton<IPaymentGateway, StubPaymentGateway>();
builder.Services.AddSingleton<IOutboxTransport, PaymentOutboxTransport>();
builder.Services.AddDaprOutbox(options =>
{
    options.Scope = "payment";
});
builder.Services.AddSingleton<IPaymentOutcomePublisher, DaprPaymentOutcomePublisher>();
builder.Services.AddSingleton<IMetricsRecorder, DaprMetricsRecorder>();
builder.Services.AddSingleton<PaymentSagaProcessor>();
builder.Services.AddSingleton<PaymentRequestedHandler>();
builder.Services.AddSingleton(serviceProvider =>
{
    var bulkheadOptions = serviceProvider.GetRequiredService<IOptions<BulkheadOptions>>().Value;
    return new BulkheadGate(bulkheadOptions.MaxConcurrency);
});
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<CorrelationIdScopeMiddleware>();
app.UseCloudEvents();

app.MapHealthRoutes();
app.MapInternalPaymentRoutes();
app.MapSubscriptionRoutes();
app.MapSubscribeHandler();

app.Run();
