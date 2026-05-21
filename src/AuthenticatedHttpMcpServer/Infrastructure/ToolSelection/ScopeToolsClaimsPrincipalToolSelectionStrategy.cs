using System.Security.Claims;
using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;

public class ScopeToolsClaimsPrincipalToolSelectionStrategy : HttpContextToolSelectionStrategy
{
  private const string Scope = "scope";
  private const string ToolPrefix = "tool:";
  public IEnumerable<McpServerTool> FilterToolsWithStrategy(IReadOnlyCollection<McpServerTool> tools, HttpContext ctx)
  {
    ClaimsPrincipal userPrincipal = ctx.User;
    if (userPrincipal.HasClaim(Scope, $"{ToolPrefix}all"))
    {
      foreach (McpServerTool tool in tools)
      {
        yield return tool;
      }

      yield break;
    }

    foreach (McpServerTool tool in tools)
    {
      if (userPrincipal.HasClaim(Scope, $"{ToolPrefix}{tool.ProtocolTool.Name}"))
      {
        yield return tool;
      }
    }
  }
}