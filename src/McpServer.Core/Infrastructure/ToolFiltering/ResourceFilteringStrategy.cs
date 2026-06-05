namespace McpServer.Infrastructure.ToolFiltering;

public interface ResourceFilteringStrategy
{
    IEnumerable<string> FilterResources(HttpContext httpContext, IEnumerable<string> resourceNames);
}
