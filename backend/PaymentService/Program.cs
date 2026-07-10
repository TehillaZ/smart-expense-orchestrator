using ApprovalFlow.Observability;
using ApprovalFlow.Reliability.Outbox;
using ApprovalFlow.Reliability.Throttling;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using PaymentService.Middleware;
using PaymentService.Routes;
using PaymentService.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddApprovalFlowObservability("PaymentService");

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
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "Payment Service API",
            Version = "v1",
            Description = "Payment saga processing, compensation, and payment event subscriptions.",
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Payment Service API")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.UseApprovalFlowObservability();
app.UseMiddleware<CorrelationIdScopeMiddleware>();
app.UseCloudEvents();

app.MapHealthRoutes();
app.MapInternalPaymentRoutes();
app.MapSubscriptionRoutes();
app.MapSubscribeHandler();

app.Run();
