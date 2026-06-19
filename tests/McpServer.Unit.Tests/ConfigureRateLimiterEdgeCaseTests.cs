using McpServer.Infrastructure;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Unit.Tests;

public class ConfigureRateLimiterEdgeCaseTests
{
  [Test]
  public async Task ConfigureRateLimiter_WhenDisabled_DoesNotRegisterMarker()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["RateLimiterOptions:Enabled"] = "false",
        })
        .Build();

    services.ConfigureRateLimiter(config);
    var provider = services.BuildServiceProvider();

    var marker = provider.GetService<ApiBuilder.RateLimiterMarker>();
    await Assert.That(marker).IsNull();
  }

  [Test]
  public async Task ConfigureRateLimiter_WhenDisabled_IsRateLimiterConfiguredReturnsFalse()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["RateLimiterOptions:Enabled"] = "false",
        })
        .Build();

    services.ConfigureRateLimiter(config);
    var provider = services.BuildServiceProvider();

    await Assert.That(provider.IsRateLimiterConfigured()).IsFalse();
  }

  [Test]
  public async Task ConfigureRateLimiter_WhenDisabled_DoesNotAddRateLimiterServices()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["RateLimiterOptions:Enabled"] = "false",
        })
        .Build();

    services.ConfigureRateLimiter(config);
    var provider = services.BuildServiceProvider();

    var opts = provider.GetService<RateLimiterOptions>();
    await Assert.That(opts).IsNull();
  }

  [Test]
  public async Task ConfigureRateLimiter_MissingFixedWindowRateLimit_Throws()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["RateLimiterOptions:Enabled"] = "true",
          ["RateLimiterOptions:McpWindowRateLimit:PermitLimit"] = "200",
          ["RateLimiterOptions:McpWindowRateLimit:Window"] = "00:00:30",
        })
        .Build();

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
    {
      services.ConfigureRateLimiter(config);
      return Task.CompletedTask;
    });
  }

  [Test]
  public async Task ConfigureRateLimiter_MissingMcpWindowRateLimit_Throws()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["RateLimiterOptions:Enabled"] = "true",
          ["RateLimiterOptions:FixedWindowRateLimit:PermitLimit"] = "50",
          ["RateLimiterOptions:FixedWindowRateLimit:Window"] = "00:01:00",
        })
        .Build();

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
    {
      services.ConfigureRateLimiter(config);
      return Task.CompletedTask;
    });
  }

  [Test]
  public async Task ConfigureRateLimiter_MissingEntireSection_Throws()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    var config = new ConfigurationBuilder().Build();

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
    {
      services.ConfigureRateLimiter(config);
      return Task.CompletedTask;
    });
  }

  [Test]
  public async Task ConfigureRateLimiter_RegistersFirstRejectionsTracker()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["RateLimiterOptions:Enabled"] = "true",
          ["RateLimiterOptions:FixedWindowRateLimit:PermitLimit"] = "50",
          ["RateLimiterOptions:FixedWindowRateLimit:Window"] = "00:01:00",
          ["RateLimiterOptions:McpWindowRateLimit:PermitLimit"] = "200",
          ["RateLimiterOptions:McpWindowRateLimit:Window"] = "00:00:30",
        })
        .Build();

    services.ConfigureRateLimiter(config);
    var provider = services.BuildServiceProvider();

    var tracker = provider.GetKeyedService<
        System.Collections.Concurrent.ConcurrentDictionary<string, DateTimeOffset>>(
        ApiBuilder.FirstRejectionsKey);
    await Assert.That(tracker).IsNotNull();
  }

  [Test]
  public async Task ConfigureRateLimiter_RegistersRateLimiterOptionsSingleton()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    var config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
          ["RateLimiterOptions:Enabled"] = "true",
          ["RateLimiterOptions:FixedWindowRateLimit:PermitLimit"] = "30",
          ["RateLimiterOptions:FixedWindowRateLimit:Window"] = "00:02:00",
          ["RateLimiterOptions:McpWindowRateLimit:PermitLimit"] = "100",
          ["RateLimiterOptions:McpWindowRateLimit:Window"] = "00:00:45",
        })
        .Build();

    services.ConfigureRateLimiter(config);
    var provider = services.BuildServiceProvider();

    var opts = provider.GetRequiredService<RateLimiterOptions>();
    await Assert.That(opts.FixedWindowRateLimit!.PermitLimit).IsEqualTo(30);
    await Assert.That(opts.McpWindowRateLimit!.PermitLimit).IsEqualTo(100);
  }
}