namespace McpServer.Infrastructure.ToolFiltering;

/// <summary>
/// Filters MCP tools based on the <c>Mcp:AllowedTools</c> configuration section.
/// Only tools whose name appears in the allowlist are included.
/// </summary>
/// <remarks>
/// When <c>Mcp:AllowedTools</c> is missing or empty, this strategy is a no-op
/// and returns all tool names unchanged.
/// </remarks>
public sealed class AppSettingsToolFilteringStrategy : ToolFilteringStrategy
{
    private static readonly string[] EmptyArray = [];

    private readonly IConfiguration _configuration;

    public AppSettingsToolFilteringStrategy(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IEnumerable<string> FilterTools(HttpContext httpContext, IEnumerable<string> toolNames)
    {
        var allowedTools = _configuration.GetSection("Mcp:AllowedTools").Get<string[]>() ?? EmptyArray;

        if (allowedTools.Length == 0)
        {
            // No allowlist configured — allow all tools
            return toolNames;
        }

        var allowedSet = new HashSet<string>(allowedTools, StringComparer.OrdinalIgnoreCase);
        return toolNames.Where(allowedSet.Contains);
    }
}
