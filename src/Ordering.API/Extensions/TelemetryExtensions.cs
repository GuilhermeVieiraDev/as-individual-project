using eShop.Ordering.API.Infrastructure.Telemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace eShop.Ordering.API.Extensions;

public static class TelemetryExtensions
{
    public static IHostApplicationBuilder AddOrderingTelemetry(this IHostApplicationBuilder builder)
    {
        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] ?? "http://localhost:4317";

        // Register our custom activity source and ensure OTLP exporter is configured
        builder.Services.ConfigureOpenTelemetryTracerProvider(tracerProviderBuilder =>
        {
            tracerProviderBuilder
                .AddSource(OrderingTelemetry.Source.Name)
                .AddProcessor<PiiRedactionProcessor>()
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService("OrderingAPI")
                        .AddTelemetrySdk()
                        .AddAttributes(new Dictionary<string, object>
                        {
                            ["deployment.environment"] = builder.Environment.EnvironmentName
                        }));
                
            tracerProviderBuilder.AddOtlpExporter(options => {
                options.Endpoint = new Uri(otlpEndpoint);
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
        });
        
        // Register our custom meter
        builder.Services.ConfigureOpenTelemetryMeterProvider(meterProviderBuilder =>
        {
            meterProviderBuilder
                .AddMeter(OrderingMetrics.Meter.Name)
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService("OrderingAPI")
                        .AddTelemetrySdk()
                        .AddAttributes(new Dictionary<string, object>
                        {
                            ["deployment.environment"] = builder.Environment.EnvironmentName
                        }));
                        
            // Ensure we have a Prometheus exporter
            meterProviderBuilder.AddPrometheusExporter();
        });
        
        return builder;
    }
}
