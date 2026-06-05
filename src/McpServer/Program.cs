using System.Text.Json;

using McpServer.Infrastructure;
using McpServer.Resources;
using McpServer.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.AddLogging();
builder.AddComplianceServices();

builder.Services
  .AddErrorHandling()
  .AddHealthChecksServices()
  .AddOAuth(builder.Configuration)
  .ConfigureRateLimiter(builder.Configuration)
  .AddMcp(builder.Configuration, mcp =>
  {
    var toolSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    toolSerializerOptions.TypeInfoResolverChain.Add(McpToolsJsonContext.Default);

    mcp.WithTools<RandomNumberTools>(toolSerializerOptions)
       .WithResources<DemoResources>();
  })
  .AddToolFiltering()
  .AddResourceFiltering();

var app = builder.Build();

app
  .UseErrorHandling()
  .UseLogging()
  .UseOAuth()
  .UseMcp()
  .UseMaps();
await app.RunAsync();
