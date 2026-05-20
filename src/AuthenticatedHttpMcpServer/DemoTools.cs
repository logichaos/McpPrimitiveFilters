using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AuthenticatedHttpMcpServer.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer;

[McpServerToolType]
public class DemoTools(IHttpContextAccessor http)
{
  [Authorize(Policy = Constants.Auth.Policies.MrAwesome)]
  [McpServerTool(Destructive = false, Idempotent = true, Name = "hello_world", Title = "Hello World", ReadOnly = true)]
  [Description("Says hello to the given name")]
  public string HelloWorld(string name = "world")
  {
    ClaimsPrincipal? user = http.HttpContext?.User;
    return $"Hello, {name}({user?.Identity?.Name ?? "anonymous"}) from your MCP server";
  }

  [Authorize(Policy = Constants.Auth.Policies.MrAwesome)]
  [McpServerTool(Destructive = false, Idempotent = true, Name = "random_number", Title = "Random Number",
    ReadOnly = true)]
  [Description("Return a message with a random number between nim and max")]
  public string RandomNumber([Required] int min = 1, [Required] int max = 100)
  {
    ClaimsPrincipal? user = http.HttpContext?.User;
    return $"Hello Random {user?.Identity?.Name}, your number is {Random.Shared.Next(min, max)}) from your MCP server";
  }
}