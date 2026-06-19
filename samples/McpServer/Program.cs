using System.Text.Json;

using McpServer.Infrastructure;
using McpServer.Prompts;
using McpServer.Resources;
using McpServer.Tools;

using ModelContextProtocol;

var builder = WebApplication.CreateBuilder(args);

builder.AddLogging();
builder.AddComplianceServices();

builder.Services
  .AddErrorHandling()
  .AddHealthChecksServices()
  .AddOAuth(builder.Configuration)
  .ConfigureRateLimiter(builder.Configuration)
  .AddMcpPrimitiveFilters()
  .AddMcp(builder.Configuration, mcp =>
  {
    var toolSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    toolSerializerOptions.TypeInfoResolverChain.Add(McpToolsJsonContext.Default);

    mcp.WithTools<RandomNumberTools>(toolSerializerOptions)
       .WithResources<DemoResources>()
       .WithPrompts<DemoPrompts>();
  });

var app = builder.Build();

var transport = builder.Configuration.GetValue<string>("MCP:Transport") ?? "http";
var isHttp = transport is "http" or "both";
var isStdio = transport is "stdio" or "both";

// Log to stderr for stdio transport so stdout stays clean for MCP protocol messages.
if (isStdio)
{
  app.Logger.LogInformation("McpServer starting with stdio transport");
}

app
  .UseErrorHandling()
  .UseLogging()
  .UseOAuth()
  .UseMcp()
  .UseMaps();

await app.RunAsync();