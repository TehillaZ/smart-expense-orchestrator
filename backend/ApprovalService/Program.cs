using ApprovalService.Routes;
using ApprovalService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDaprClient();
builder.Services.AddSingleton<ISubmissionStateStore, DaprSubmissionStateStore>();
builder.Services.AddSingleton<IPolicySettingsStore, DaprPolicySettingsStore>();
builder.Services.AddSingleton<IInvoiceAiProvider, StubInvoiceAiProvider>();
builder.Services.AddSingleton<PolicyEngine>();
builder.Services.AddSingleton<ApprovalWorkflowProcessor>();
builder.Services.AddSingleton<ApprovalDecisionHandler>();
builder.Services.AddSingleton<SubmissionReceivedHandler>();
builder.Services.AddSingleton<IPaymentRequestPublisher, DaprPaymentRequestPublisher>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCloudEvents();

app.MapApprovalRoutes();
app.MapInternalApprovalRoutes();
app.MapPolicyRoutes();
app.MapHealthRoutes();
app.MapSubscriptionRoutes();
app.MapSubscribeHandler();

app.Run();
