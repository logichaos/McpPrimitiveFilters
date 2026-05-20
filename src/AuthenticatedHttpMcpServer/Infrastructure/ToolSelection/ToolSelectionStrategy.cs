using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;

public interface ToolSelectionStrategy<in T>
{
  IEnumerable<McpServerTool> FilterToolsWithStrategy(IReadOnlyCollection<McpServerTool> tools, T input);
}

public interface HttpContextToolSelectionStrategy : ToolSelectionStrategy<HttpContext>;