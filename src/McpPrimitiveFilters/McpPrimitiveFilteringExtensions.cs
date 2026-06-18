using McpPrimitiveFilters;
using McpPrimitiveFilters.Strategies;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Microsoft.Extensions.DependencyInjection;

public static class McpPrimitiveFiltersExtensions
{
    public static IServiceCollection AddMcpPrimitiveFilters(
        this IServiceCollection services,
        Action<McpPrimitiveFiltersOptions>? configure = null)
    {
        services.Configure(configure ?? (_ => { }));

        services.TryAddSingleton<
            McpPrimitiveFilteringStrategy, AppSettingsPrimitiveFilteringStrategy>();
        services.TryAddSingleton<
            McpPrimitiveFilteringStrategy, OAuthClaimsFilteringStrategy>();

        services.AddSingleton<IConfigureOptions<McpServerOptions>, ToolFilterConfigurator>();
        services.AddSingleton<IConfigureOptions<McpServerOptions>, ResourceFilterConfigurator>();
        services.AddSingleton<IConfigureOptions<McpServerOptions>, PromptFilterConfigurator>();

        return services;
    }
}
