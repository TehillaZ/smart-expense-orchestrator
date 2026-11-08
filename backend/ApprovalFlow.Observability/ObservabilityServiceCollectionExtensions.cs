using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ApprovalFlow.Observability;

public static class ObservabilityServiceCollectionExtensions
{
    public const string PrometheusMetricsPath = "/metrics";

    public static WebApplicationBuilder AddApprovalFlowObservability(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        var options = builder.Configuration
            .GetSection(ObservabilityOptions.SectionName)
            .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

        builder.Services.Configure<ObservabilityOptions>(
            builder.Configuration.GetSection(ObservabilityOptions.SectionName));
        builder.Services.AddSingleton<ObservabilityMetrics>();

        if (!options.Enabled)
        {
            return builder;
        }

        var otlpEndpoint = new Uri(options.OtlpEndpoint);

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(aspNetCoreOptions =>
                {
                    aspNetCoreOptions.RecordException = true;
                    aspNetCoreOptions.Filter = httpContext =>
                        !httpContext.Request.Path.StartsWithSegments(PrometheusMetricsPath);
                    aspNetCoreOptions.EnrichWithHttpRequest = (activity, request) =>
                    {
                        EnrichWithCorrelationId(activity, request.Headers[CorrelationConstants.HeaderName].FirstOrDefault());
                    };
                    aspNetCoreOptions.EnrichWithHttpResponse = (activity, response) =>
                    {
                        activity.SetTag("http.response.status_code", response.StatusCode);
                    };
                })
                .AddHttpClientInstrumentation()
                .AddSource(ObservabilityActivitySources.Name)
                .AddOtlpExporter(otlpExporterOptions =>
                {
                    otlpExporterOptions.Endpoint = otlpEndpoint;
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(ObservabilityMetrics.MeterName)
                .AddPrometheusExporter()
                .AddOtlpExporter(otlpExporterOptions =>
                {
                    otlpExporterOptions.Endpoint = otlpEndpoint;
                }))
            .WithLogging(logging => logging
                .AddOtlpExporter(otlpExporterOptions =>
                {
                    otlpExporterOptions.Endpoint = otlpEndpoint;
                }));

        return builder;
    }

    public static WebApplication UseApprovalFlowObservability(this WebApplication app)
    {
        var options = app.Configuration
            .GetSection(ObservabilityOptions.SectionName)
            .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

        if (!options.Enabled)
        {
            return app;
        }

        app.UseMiddleware<RequestMetricsMiddleware>();
        app.MapPrometheusScrapingEndpoint(PrometheusMetricsPath);
        return app;
    }

    private static void EnrichWithCorrelationId(Activity activity, string? correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return;
        }

        activity.SetTag("correlation.id", correlationId);
        activity.AddBaggage(CorrelationConstants.HeaderName, correlationId);
    }
}
