using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;

public class ToolsOptionsToolSelectionStrategy(IOptionsMonitor<ToolsSelectionOptions> toolsSelectionOptions) : ToolSelectionStrategy
{
  public IEnumerable<McpServerTool> FilterTools(IEnumerable<McpServerTool> tools)
  {
    if (toolsSelectionOptions.CurrentValue.AllowedTools is null or { Length: 0 })
      return tools;
    return tools.Where(tool => toolsSelectionOptions.CurrentValue.AllowedTools.Contains(tool.ProtocolTool.Name));
  }
}

public class ToolsSelectionOptions
{
  public const string ToolsSelection = "ToolsSelection";

  public string[]? AllowedTools { get; set; }
}