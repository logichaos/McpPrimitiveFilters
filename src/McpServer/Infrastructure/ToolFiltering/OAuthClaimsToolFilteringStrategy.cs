using System.Security.Claims;

namespace McpServer.Infrastructure.ToolFiltering;

/// <summary>
/// Filters MCP tools based on OAuth/JWT claims of the form <c>mcp.tool.{tool_name}</c>.
/// A tool is included if and only if the authenticated user has a claim with that
/// exact key and the value <c>"true"</c>.
/// </summary>
/// <remarks>
/// When the user is not authenticated, this strategy is a no-op and returns
/// all tool names unchanged.
/// </remarks>
public sealed class OAuthClaimsToolFilteringStrategy : ToolFilteringStrategy
{
    public IEnumerable<string> FilterTools(HttpContext httpContext, IEnumerable<string> toolNames)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            // No authenticated user — no claims to check, allow all tools
            return toolNames;
        }

        var principal = httpContext.User;
        return toolNames.Where(name =>
        {
            var claimType = $"mcp.tool.{name}";
            var claim = principal.FindFirst(claimType);
            return claim is not null && string.Equals(claim.Value, "true", StringComparison.Ordinal);
        });
    }
}
