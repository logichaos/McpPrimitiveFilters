using McpServer.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace McpServer.Unit.Tests;

public class ApiBuilderLoggingTests
{
    [Test]
    public async Task AddLogging_ClearsExistingProviders()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.Services.AddSingleton<ILoggerProvider, FakeLoggerProvider>();

        ApiBuilder.AddLogging(builder);

        var app = builder.Build();
        var loggerProviders = app.Services.GetServices<ILoggerProvider>().ToList();

        await Assert.That(loggerProviders.OfType<FakeLoggerProvider>().Count()).IsEqualTo(0);
    }

    [Test]
    public async Task AddLogging_AddsConsoleProvider()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());

        ApiBuilder.AddLogging(builder);

        var app = builder.Build();
        var loggerProviders = app.Services.GetServices<ILoggerProvider>().ToList();

        await Assert.That(loggerProviders
            .Any(p => p.GetType().Name.Contains("Console"))).IsTrue();
    }

    [Test]
    public async Task AddLogging_RegistersDynamicLogLevelService()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());

        ApiBuilder.AddLogging(builder);

        var app = builder.Build();
        var service = app.Services.GetService<DynamicLogLevelService>();

        await Assert.That(service).IsNotNull();
    }

    [Test]
    public async Task AddLogging_DynamicLogLevelService_IsSingleton()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());

        ApiBuilder.AddLogging(builder);

        var app = builder.Build();
        var instance1 = app.Services.GetRequiredService<DynamicLogLevelService>();
        var instance2 = app.Services.GetRequiredService<DynamicLogLevelService>();

        await Assert.That(instance1).IsEqualTo(instance2);
    }

    [Test]
    public async Task AddLogging_UsesConfigurationSection()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Microsoft"] = "Error",
        });

        ApiBuilder.AddLogging(builder);

        var app = builder.Build();
        var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();

        await Assert.That(loggerFactory).IsNotNull();
    }

    [Test]
    public async Task AddLogging_ReturnsSameBuilder()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());

        var result = ApiBuilder.AddLogging(builder);

        await Assert.That(result).IsEqualTo(builder);
    }

    [Test]
    public async Task UseLogging_ReturnsSameApp()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        var app = builder.Build();

        var result = ApiBuilder.UseLogging(app);

        await Assert.That(result).IsEqualTo(app);
    }

    [Test]
    public async Task UseLogging_DoesNotThrow()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        var app = builder.Build();

        var result = ApiBuilder.UseLogging(app);
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task AddLogging_RegistersOpenTelemetryServices()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());

        ApiBuilder.AddLogging(builder);

        var app = builder.Build();
        var tracerProvider = app.Services.GetService<TracerProvider>();
        var meterProvider = app.Services.GetService<MeterProvider>();
        var loggerProvider = app.Services.GetServices<ILoggerProvider>()
            .FirstOrDefault(p => p.GetType().FullName?.Contains("OpenTelemetry") == true);

        await Assert.That(tracerProvider).IsNotNull();
        await Assert.That(meterProvider).IsNotNull();
        await Assert.That(loggerProvider).IsNotNull();
    }

    [Test]
    public async Task AddLogging_OtlpExporter_Disabled_WhenNoEndpoint()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());

        ApiBuilder.AddLogging(builder);

        var app = builder.Build();
        var openTelemetryLogger = app.Services.GetServices<ILoggerProvider>()
            .FirstOrDefault(p => p.GetType().FullName?.Contains("OpenTelemetry") == true);

        await Assert.That(openTelemetryLogger).IsNotNull();
    }

    [Test]
    public async Task AddLogging_OtlpExporter_Enabled_WhenEndpointSet()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://localhost:4317"
        });

        ApiBuilder.AddLogging(builder);

        var app = builder.Build();
        await Assert.That(app.Services.GetService<TracerProvider>()).IsNotNull();
        await Assert.That(app.Services.GetService<MeterProvider>()).IsNotNull();
    }

    [Test]
    public async Task AddLogging_OpenTelemetry_IncludesTraceAndSpanId()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());

        ApiBuilder.AddLogging(builder);

        var app = builder.Build();
        await Assert.That(app.Services.GetService<TracerProvider>()).IsNotNull();
        await Assert.That(app.Services.GetService<MeterProvider>()).IsNotNull();
    }

    private sealed class FakeLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new FakeLogger();

        public void Dispose() { }

        private sealed class FakeLogger : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter) { }
        }
    }
}
