using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;
using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer.Infrastructure;

public sealed class McpToolRegistry(IServiceProvider services)
{
  private IReadOnlyCollection<McpServerTool> GetToolsFromClass<
    [DynamicallyAccessedMembers(
      DynamicallyAccessedMemberTypes.PublicMethods)]
    T>()
  {
    List<McpServerTool> allTools = new();
    Type toolType = typeof(T);
    T target = ActivatorUtilities.CreateInstance<T>(services);

    foreach (MethodInfo method in toolType.GetMethods()
               .Where(m => m.GetCustomAttributes<McpServerToolAttribute>().Any()))
    {
      try
      {
        McpServerTool mcpServerTool = McpServerTool.Create(method, target, new McpServerToolCreateOptions());
        allTools.Add(mcpServerTool);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to add tool {toolType.Name}.{method.Name}: {ex.Message}");
      }
    }

    return allTools.AsReadOnly();
  }

  public IEnumerable<McpServerTool> GetToolsForClaimsPrincipal(HttpContext ctx,
    HttpContextToolSelectionStrategy strategy)
  {
    IReadOnlyCollection<McpServerTool> demoTools = GetToolsFromClass<DemoTools>();
    return strategy.FilterToolsWithStrategy([.. demoTools], ctx);
  }
}