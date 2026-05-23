using System.Security.Claims;
using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;

public class ScopeToolsClaimsPrincipalToolSelectionStrategy(IHttpContextAccessor httpContextAccessor)
  : ToolSelectionStrategy
{
  private const string Scope = "scope";
  private const string ToolPrefix = "tool:";

  public IEnumerable<McpServerTool> FilterTools(IEnumerable<McpServerTool> tools)
  {
    ClaimsPrincipal userPrincipal = httpContextAccessor.HttpContext!.User;

    // Collect all granted scopes. Scopes may be emitted as a single
    // space-separated string (e.g. "mcp:tools mcp:tools.hello_world mcp:tools.random_number")
    // or as multiple individual claims. We handle both by splitting on space.
    var grantedScopes = new HashSet<string>(StringComparer.Ordinal);
    foreach (var scopeClaim in userPrincipal.FindAll(Scope))
    {
      foreach (var scope in scopeClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
      {
        grantedScopes.Add(scope);
      }
    }

    // "tool:all" means all tools are granted.
    if (grantedScopes.Contains($"{ToolPrefix}all"))
    {
      return tools;
    }

    return tools.Where(tool => grantedScopes.Contains($"{ToolPrefix}{tool.ProtocolTool.Name}"));
  }
}
