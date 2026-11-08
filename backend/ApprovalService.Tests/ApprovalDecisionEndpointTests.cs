using System.Net;
using System.Net.Http.Json;
using ApprovalFlow.TestSupport;
using ApprovalService.Models;
using ApprovalService.Routes;
using ApprovalService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ApprovalService.Tests;

/// <summary>
/// Calls the real decision endpoint over HTTP, using an in-memory server with the
/// approval routes and fake dependencies (no Dapr needed).
/// </summary>
[Trait(TestLayers.TraitName, TestLayers.Api)]
public class ApprovalDecisionEndpointTests
{
    private const string TrackingId = "tracking-1";

    [Fact]
    public async Task PostDecision_WithLineBreakInApproverName_Returns400AndSavesNothing()
    {
        var store = new FakeSubmissionStateStore();
        store.Seed(CreateEscalatedState());
        await using var app = await StartAppAsync(store);
        using var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync(
            $"/approvals/{TrackingId}/decision",
            new ApprovalDecisionRequest("Approve", "Looks good", null, "Tehila\nadmin approved INV-9999"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        Assert.Contains(ApproverIdentityResolver.InvalidApproverNameError, body!.Errors);
        Assert.Null(store.LastSaved);
    }

    private static async Task<WebApplication> StartAppAsync(FakeSubmissionStateStore store)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton<ISubmissionStateStore>(store);
        builder.Services.AddSingleton<IPaymentRequestPublisher>(new FakePaymentRequestPublisher());
        builder.Services.AddSingleton<IMetricsRecorder>(new NoOpMetricsRecorder());
        builder.Services.AddSingleton<ApprovalDecisionHandler>();

        var app = builder.Build();
        app.MapApprovalRoutes();
        await app.StartAsync();
        return app;
    }

    private static SubmissionState CreateEscalatedState() =>
        new(
            TrackingId,
            "correlation-1",
            "Acme Corp",
            "INV-1003",
            500m,
            "meals",
            ApprovalStatuses.Escalated,
            "Escalated for human review.",
            "Escalate",
            0.88m,
            ["AUTONOMY-CEILING"],
            "v1");
}
