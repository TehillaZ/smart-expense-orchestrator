using ApprovalFlow.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ApprovalFlow.Observability.Tests;

[Collection("Observability")]
public class PrometheusScrapingEndpointTests
{
    [Fact]
    public async Task MetricsEndpoint_ExposesCustomAndRuntimeMeters()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Observability:Enabled"] = "true";
        builder.Configuration["Observability:OtlpEndpoint"] = "http://127.0.0.1:4317";
        builder.AddApprovalFlowObservability("PrometheusTestService");

        await using var app = builder.Build();
        app.UseApprovalFlowObservability();
        app.MapGet("/ping", () => Results.Ok("pong"));
        await app.StartAsync();

        var metrics = app.Services.GetRequiredService<ObservabilityMetrics>();
        metrics.RecordRequest("prometheus.verify", succeeded: true);
        metrics.RecordRequest("prometheus.verify", succeeded: false);

        var client = app.GetTestClient();
        await client.GetAsync("/ping");

        // Allow the Prometheus exporter to observe recent measurements.
        await Task.Delay(250);

        var response = await client.GetAsync(ObservabilityServiceCollectionExtensions.PrometheusMetricsPath);
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, body);
        Assert.Contains("approvalflow_requests_total", body);
        Assert.Contains("approvalflow_requests_failures", body);
        // OpenTelemetry.Instrumentation.Runtime exposes meters such as dotnet_gc_* / http_server_*.
        Assert.True(
            body.Contains("dotnet_gc_", StringComparison.Ordinal)
            || body.Contains("http_server_", StringComparison.Ordinal),
            "Expected ASP.NET or .NET runtime metrics in /metrics payload.");
    }
}
