using System.Security.Claims;
using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;

public class ScopeToolsClaimsPrincipalToolSelectionStrategy : HttpContextToolSelectionStrategy
{
  private const string SCOPE = "scope";
  private const string TOOLPREFIX = "tool:";
  public IEnumerable<McpServerTool> FilterToolsWithStrategy(IReadOnlyCollection<McpServerTool> tools, HttpContext ctx)
  {
    ClaimsPrincipal userPrincipal = ctx.User;
    if (userPrincipal.HasClaim(SCOPE, $"{TOOLPREFIX}all"))
    {
      foreach (McpServerTool tool in tools)
      {
        yield return tool;
      }

      yield break;
    }

    foreach (McpServerTool tool in tools)
    {
      if (userPrincipal.HasClaim(SCOPE, $"{TOOLPREFIX}{tool.ProtocolTool.Name}"))
      {
        yield return tool;
      }
    }
  }
}