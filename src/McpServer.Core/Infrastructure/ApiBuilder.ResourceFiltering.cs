using McpServer.Infrastructure.ToolFiltering;

namespace McpServer.Infrastructure;

public static partial class ApiBuilder
{
    public static IServiceCollection AddResourceFiltering(this IServiceCollection services)
    {
        services.AddSingleton<ResourceFilteringStrategy, OAuthClaimsResourceFilteringStrategy>();
        services.AddSingleton<ResourceFilteringStrategy, AppSettingsResourceFilteringStrategy>();

        return services;
    }
}
