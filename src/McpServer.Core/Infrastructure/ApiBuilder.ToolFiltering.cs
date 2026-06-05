using McpServer.Infrastructure.ToolFiltering;

namespace McpServer.Infrastructure;

public static partial class ApiBuilder
{
    /// <summary>
    /// Registers the built-in tool filtering strategies and enables the tool
    /// filtering pipeline on both <c>ListTools</c> and <c>CallTool</c> requests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call this after <see cref="AddMcp"/> to register the OAuth claims and
    /// AppSettings allowlist strategies. Additional custom strategies can be
    /// registered via <c>services.AddSingleton&lt;IToolFilteringStrategy, T&gt;()</c>.
    /// </para>
    /// <para>
    /// All registered strategies are applied in DI registration order with AND
    /// semantics: a tool must pass every strategy to be visible or invocable.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddToolFiltering(this IServiceCollection services)
    {
        // Register IHttpContextAccessor so filter delegates can access the
        // current HttpContext (required for claims-based strategies).
        services.AddHttpContextAccessor();

        // Register built-in strategies
        services.AddSingleton<ToolFilteringStrategy, OAuthClaimsToolFilteringStrategy>();
        services.AddSingleton<ToolFilteringStrategy, AppSettingsToolFilteringStrategy>();

        return services;
    }
}
