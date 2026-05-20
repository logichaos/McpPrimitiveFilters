using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Claims;

using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer.Infrastructure;

public sealed class McpToolRegistry
{
  private static ConcurrentBag<McpServerTool> AllTools { get; } = new();

  public McpToolRegistry(IServiceProvider services)
  {
    InitializeAllTools<DemoTools>(services);
  }

  static void InitializeAllTools<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
      System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
    T>(IServiceProvider services)
  {
    var toolType = typeof(T);
    var target = ActivatorUtilities.CreateInstance<T>(services);

    foreach (var method in toolType.GetMethods()
               .Where(m => m.GetCustomAttributes<McpServerToolAttribute>().Any()))
    {
      try
      {
        McpServerTool mcpServerTool = McpServerTool.Create(method, target, new McpServerToolCreateOptions());
        AllTools.Add(mcpServerTool);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to add tool {toolType.Name}.{method.Name}: {ex.Message}");
      }
    }
  }

  public IList<McpServerTool> GetToolsForClaimsPrincipal(ClaimsPrincipal user)
  {
    var userTools = new List<McpServerTool>();
    foreach (var tool in AllTools)
    {
      if(user.HasClaim("scope", $"tool:{tool.ProtocolTool.Name}"))
        userTools.Add(tool);
    }

    return userTools;
      
    //return AllTools.Where(tool => user.HasClaim("Tool", tool.ProtocolTool.Name)).ToList();
  }
}