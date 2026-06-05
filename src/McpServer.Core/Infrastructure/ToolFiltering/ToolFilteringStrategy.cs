namespace McpServer.Infrastructure.ToolFiltering;

/// <summary>
/// A pluggable strategy for filtering which MCP server tools are visible
/// and invocable by a given client request.
/// </summary>
/// <remarks>
/// All registered <see cref="ToolFilteringStrategy"/> implementations are
/// resolved from DI and applied in registration order (AND semantics):
/// a tool must pass every strategy to be included.
/// </remarks>
public interface ToolFilteringStrategy
{
    /// <summary>
    /// Filters the provided tool names, returning only the names of tools
    /// that should be available for the current HTTP request.
    /// </summary>
    /// <param name="httpContext">The current HTTP context, providing access to the authenticated user and request services.</param>
    /// <param name="toolNames">The full list of registered MCP server tool names.</param>
    /// <returns>The filtered subset of tool names allowed for this request.</returns>
    IEnumerable<string> FilterTools(HttpContext httpContext, IEnumerable<string> toolNames);
}
