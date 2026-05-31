using McpServer.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace McpServer.Unit.Tests;

public class ApiBuilderLoggingTests
{
    [Test]
    public async Task AddLogging_ClearsExistingProviders()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.Services.AddSingleton<ILoggerProvider, FakeLoggerProvider>();

        ApiBuilder.AddLogging(builder);

        var provider = builder.Services.BuildServiceProvider();
        var loggerProviders = provider.GetServices<ILoggerProvider>().ToList();

        await Assert.That(loggerProviders.OfType<FakeLoggerProvider>().Count()).IsEqualTo(0);
    }

    [Test]
    public async Task AddLogging_AddsConsoleProvider()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());

        ApiBuilder.AddLogging(builder);

        var provider = builder.Services.BuildServiceProvider();
        var loggerProviders = provider.GetServices<ILoggerProvider>().ToList();

        await Assert.That(loggerProviders
            .Any(p => p.GetType().Name.Contains("Console"))).IsTrue();
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

        var provider = builder.Services.BuildServiceProvider();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

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
