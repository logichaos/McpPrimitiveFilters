using McpPrimitiveFilters;
using McpPrimitiveFilters.Strategies;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Server;

namespace McpPrimitiveFilters.Unit.Tests;

public class McpPrimitiveFiltersExtensionsTests
{
  private static ServiceCollection CreateServices()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton<ILoggerFactory>(_ => NullLoggerFactory.Instance);
    services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
    return services;
  }

  [Test]
  public async Task DefaultRegistration_RegistersBothStrategies()
  {
    var services = CreateServices();
    services.AddMcpPrimitiveFilters();
    var sp = services.BuildServiceProvider();

    var strategies = sp.GetServices<McpPrimitiveFilteringStrategy>().ToList();

    await Assert.That(strategies).Count().IsEqualTo(2);
    await Assert.That(strategies.Any(s => s is AppSettingsPrimitiveFilteringStrategy)).IsTrue();
    await Assert.That(strategies.Any(s => s is OAuthClaimsFilteringStrategy)).IsTrue();
  }

  [Test]
  public async Task DefaultRegistration_RegistersAllConfigurators()
  {
    var services = CreateServices();
    services.AddMcpPrimitiveFilters();
    var sp = services.BuildServiceProvider();

    var configurators = sp.GetServices<IConfigureOptions<McpServerOptions>>().ToList();

    await Assert.That(configurators).Count().IsEqualTo(3);
    await Assert.That(configurators.Any(c => c is ToolFilterConfigurator)).IsTrue();
    await Assert.That(configurators.Any(c => c is ResourceFilterConfigurator)).IsTrue();
    await Assert.That(configurators.Any(c => c is PromptFilterConfigurator)).IsTrue();
  }

  [Test]
  public async Task DefaultRegistration_OptionsHaveExpectedDefaults()
  {
    var services = CreateServices();
    services.AddMcpPrimitiveFilters();
    var sp = services.BuildServiceProvider();

    var options = sp.GetRequiredService<IOptions<McpPrimitiveFiltersOptions>>().Value;

    await Assert.That(options.UseBuiltinAppSettingsFilteringStrategy).IsTrue();
    await Assert.That(options.UseBuiltinOAuthClaimsFilteringStrategy).IsTrue();
    await Assert.That(options.FilterTools).IsTrue();
    await Assert.That(options.FilterResources).IsTrue();
    await Assert.That(options.FilterPrompts).IsTrue();
  }

  [Test]
  public async Task DisableAppSettingsStrategy_OnlyRegistersOAuthStrategy()
  {
    var services = CreateServices();
    services.AddMcpPrimitiveFilters(o =>
        o.UseBuiltinAppSettingsFilteringStrategy = false);
    var sp = services.BuildServiceProvider();

    var strategies = sp.GetServices<McpPrimitiveFilteringStrategy>().ToList();

    await Assert.That(strategies).Count().IsEqualTo(1);
    await Assert.That(strategies[0]).IsTypeOf<OAuthClaimsFilteringStrategy>();
    await Assert.That(strategies.Any(s => s is AppSettingsPrimitiveFilteringStrategy)).IsFalse();
  }

  [Test]
  public async Task DisableOAuthStrategy_OnlyRegistersAppSettingsStrategy()
  {
    var services = CreateServices();
    services.AddMcpPrimitiveFilters(o =>
        o.UseBuiltinOAuthClaimsFilteringStrategy = false);
    var sp = services.BuildServiceProvider();

    var strategies = sp.GetServices<McpPrimitiveFilteringStrategy>().ToList();

    await Assert.That(strategies).Count().IsEqualTo(1);
    await Assert.That(strategies[0]).IsTypeOf<AppSettingsPrimitiveFilteringStrategy>();
    await Assert.That(strategies.Any(s => s is OAuthClaimsFilteringStrategy)).IsFalse();
  }

  [Test]
  public async Task DisableBothStrategies_RegistersNoStrategies()
  {
    var services = CreateServices();
    services.AddMcpPrimitiveFilters(o =>
    {
      o.UseBuiltinAppSettingsFilteringStrategy = false;
      o.UseBuiltinOAuthClaimsFilteringStrategy = false;
    });
    var sp = services.BuildServiceProvider();

    var strategies = sp.GetServices<McpPrimitiveFilteringStrategy>().ToList();

    await Assert.That(strategies).Count().IsEqualTo(0);
  }

  [Test]
  public async Task DisableBothStrategies_ConfiguratorsStillRegistered()
  {
    var services = CreateServices();
    services.AddMcpPrimitiveFilters(o =>
    {
      o.UseBuiltinAppSettingsFilteringStrategy = false;
      o.UseBuiltinOAuthClaimsFilteringStrategy = false;
    });
    var sp = services.BuildServiceProvider();

    var configurators = sp.GetServices<IConfigureOptions<McpServerOptions>>().ToList();

    await Assert.That(configurators).Count().IsEqualTo(3);
  }

  [Test]
  public async Task ConfigureAction_ForwardsOptionValues()
  {
    var services = CreateServices();
    services.AddMcpPrimitiveFilters(o =>
    {
      o.FilterTools = false;
      o.FilterResources = true;
      o.FilterPrompts = false;
    });
    var sp = services.BuildServiceProvider();

    var options = sp.GetRequiredService<IOptions<McpPrimitiveFiltersOptions>>().Value;

    await Assert.That(options.FilterTools).IsFalse();
    await Assert.That(options.FilterResources).IsTrue();
    await Assert.That(options.FilterPrompts).IsFalse();
  }

  [Test]
  public async Task CustomStrategy_RegisteredAfterAddMcpPrimitiveFilters_CoexistsWithBuiltins()
  {
    var services = CreateServices();
    services.AddMcpPrimitiveFilters();
    services.AddSingleton<McpPrimitiveFilteringStrategy, CustomTestStrategy>();
    var sp = services.BuildServiceProvider();

    var strategies = sp.GetServices<McpPrimitiveFilteringStrategy>().ToList();

    await Assert.That(strategies).Count().IsEqualTo(3);
    await Assert.That(strategies.Any(s => s is AppSettingsPrimitiveFilteringStrategy)).IsTrue();
    await Assert.That(strategies.Any(s => s is OAuthClaimsFilteringStrategy)).IsTrue();
    await Assert.That(strategies.Any(s => s is CustomTestStrategy)).IsTrue();
  }

  [Test]
  public async Task CustomStrategy_AfterAddWithDisabledAppSettings_OmitsAppSettings()
  {
    var services = CreateServices();
    services.AddMcpPrimitiveFilters(o =>
        o.UseBuiltinAppSettingsFilteringStrategy = false);
    services.AddSingleton<McpPrimitiveFilteringStrategy, CustomTestStrategy>();
    var sp = services.BuildServiceProvider();

    var strategies = sp.GetServices<McpPrimitiveFilteringStrategy>().ToList();

    await Assert.That(strategies).Count().IsEqualTo(2);
    await Assert.That(strategies.Any(s => s is OAuthClaimsFilteringStrategy)).IsTrue();
    await Assert.That(strategies.Any(s => s is CustomTestStrategy)).IsTrue();
    await Assert.That(strategies.Any(s => s is AppSettingsPrimitiveFilteringStrategy)).IsFalse();
  }

  private sealed class CustomTestStrategy : McpPrimitiveFilteringStrategy
  {
    protected override IEnumerable<string> FilterTools(IEnumerable<string> n) => n;
  }
}