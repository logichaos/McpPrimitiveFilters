namespace McpServer.Infrastructure.ToolFiltering;

public sealed class AppSettingsResourceFilteringStrategy : ResourceFilteringStrategy
{
    private static readonly string[] EmptyArray = [];

    private readonly IConfiguration _configuration;

    public AppSettingsResourceFilteringStrategy(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IEnumerable<string> FilterResources(HttpContext httpContext, IEnumerable<string> resourceNames)
    {
        var allowedResources = _configuration.GetSection("Mcp:AllowedResources").Get<string[]>() ?? EmptyArray;

        if (allowedResources.Length == 0)
        {
            return resourceNames;
        }

        var allowedSet = new HashSet<string>(allowedResources, StringComparer.OrdinalIgnoreCase);
        return resourceNames.Where(allowedSet.Contains);
    }
}
