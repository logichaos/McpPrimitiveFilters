using AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;
using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer.Infrastructure;

public sealed class McpToolRegistry(IEnumerable<McpServerTool> tools)
{
  public IEnumerable<McpServerTool> FilterToolsUsingStrategy(IEnumerable<ToolSelectionStrategy> strategies)
  {
    IEnumerable<McpServerTool> finalSelection = tools;
    foreach (var strategy in strategies)
      finalSelection = strategy.FilterTools(finalSelection);
    return finalSelection;
  }
}
