using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace McpServer.Infrastructure;

public static partial class ApiBuilder
{
    public static WebApplicationBuilder AddLogging(this WebApplicationBuilder builder)
    {
        var levelService = new DynamicLogLevelService();
        builder.Services.AddSingleton(levelService);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        builder.Logging.AddFilter((category, level) => level >= levelService.MinLevel);

        AddOpenTelemetry(builder);

        return builder;
    }

    public static WebApplication UseLogging(this WebApplication app)
    {
        return app;
    }

    private static void AddOpenTelemetry(WebApplicationBuilder builder)
    {
        var otel = builder.Services.AddOpenTelemetry();

        otel.WithLogging(logging => { }, options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
        });

        otel.WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation();
            metrics.AddHttpClientInstrumentation();
            metrics.AddMeter("Microsoft.AspNetCore.Hosting");
            metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
        });

        otel.WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation();
            tracing.AddHttpClientInstrumentation();
        });

        var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            otel.UseOtlpExporter();
        }
    }
}
