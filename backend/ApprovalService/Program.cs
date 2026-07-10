using ApprovalFlow.Observability;
using ApprovalFlow.Reliability.Outbox;
using ApprovalService.Middleware;
using ApprovalService.Routes;
using ApprovalService.Services;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddApprovalFlowObservability("ApprovalService");

builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);

builder.Services.AddDaprClient();
builder.Services.AddSingleton<ISubmissionStateStore, DaprSubmissionStateStore>();
builder.Services.AddSingleton<IPolicySettingsStore, DaprPolicySettingsStore>();
builder.Services.AddSingleton<DaprSecretsProvider>();
builder.Services.AddHostedService<DaprSecretsBootstrapper>();
builder.Services.AddConfiguredInvoiceAiProvider(builder.Configuration);
builder.Services.AddSingleton<PolicyEngine>();
builder.Services.AddSingleton<ApprovalWorkflowProcessor>();
builder.Services.AddSingleton<ApprovalDecisionHandler>();
builder.Services.AddSingleton<SubmissionReceivedHandler>();
builder.Services.AddSingleton<IOutboxTransport, ApprovalOutboxTransport>();
builder.Services.AddDaprOutbox(options =>
{
    options.Scope = "approval";
});
builder.Services.AddSingleton<IPaymentRequestPublisher, DaprPaymentRequestPublisher>();
builder.Services.AddSingleton<IMetricsRecorder, DaprMetricsRecorder>();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "Approval Service API",
            Version = "v1",
            Description = "Policy evaluation, escalation queue, human decisions, and policy settings.",
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Approval Service API")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.UseApprovalFlowObservability();
app.UseMiddleware<CorrelationIdScopeMiddleware>();
app.UseCloudEvents();

app.MapApprovalRoutes();
app.MapInternalApprovalRoutes();
app.MapPolicyRoutes();
app.MapHealthRoutes();
app.MapSubscriptionRoutes();
app.MapSubscribeHandler();

app.Run();
