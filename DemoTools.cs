using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer;

[McpServerToolType]
public class DemoTools
{
  private readonly IHttpContextAccessor _http;
  public DemoTools(IHttpContextAccessor http)
  {
    _http = http;
  }

  [Authorize(Policy = "mrawesome")]
  [McpServerTool, Description("Says hello to the given name")]
  public string HelloWorld(string name = "world")
  {
    var user  = _http.HttpContext?.User;
    return $"Hello, {name}({user?.Identity?.Name ?? "anonymous"}) from your MCP server";
  }
}


