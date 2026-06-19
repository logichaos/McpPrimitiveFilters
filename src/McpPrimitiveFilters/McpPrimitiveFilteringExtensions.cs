using McpPrimitiveFilters;
using McpPrimitiveFilters.Strategies;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Server;

namespace Microsoft.Extensions.DependencyInjection;

public static class McpPrimitiveFiltersExtensions
{
  public static IServiceCollection AddMcpPrimitiveFilters(
      this IServiceCollection services,
      IConfiguration configuration,
      Action<McpPrimitiveFiltersOptions>? configure = null)
  {
    services.Configure<McpFilteringOptions>(configuration.GetSection("McpFiltering"));
    return services.AddMcpPrimitiveFilters(configure);
  }

  public static IServiceCollection AddMcpPrimitiveFilters(
      this IServiceCollection services,
      Action<McpPrimitiveFiltersOptions>? configure = null)
  {
    var options = new McpPrimitiveFiltersOptions();
    configure?.Invoke(options);
    services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

    services.AddHttpContextAccessor();

    if (options.UseBuiltinAppSettingsFilteringStrategy)
    {
      services.TryAddEnumerable(ServiceDescriptor.Singleton<
          McpPrimitiveFilteringStrategy, AppSettingsPrimitiveFilteringStrategy>());
    }
    if (options.UseBuiltinOAuthClaimsFilteringStrategy)
    {
      services.TryAddEnumerable(ServiceDescriptor.Singleton<
          McpPrimitiveFilteringStrategy, OAuthClaimsFilteringStrategy>());
    }

    services.AddSingleton<IConfigureOptions<McpServerOptions>, ToolFilterConfigurator>();
    services.AddSingleton<IConfigureOptions<McpServerOptions>, ResourceFilterConfigurator>();
    services.AddSingleton<IConfigureOptions<McpServerOptions>, PromptFilterConfigurator>();

    return services;
  }
}