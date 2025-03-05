using eShop.Ordering.API.Infrastructure.Telemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace eShop.Ordering.API.Extensions;

public static class TelemetryExtensions
{
    public static IHostApplicationBuilder AddOrderingTelemetry(this IHostApplicationBuilder builder)
    {
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
                options.Endpoint = new Uri("http://localhost:4317");
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
            });
        });
        
        return builder;
    }
}
