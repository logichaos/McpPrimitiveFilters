using System.Text.Json;

using McpServer.Infrastructure;
using McpServer.Infrastructure.OAuth;
using McpServer.Prompts;
using McpServer.Resources;
using McpServer.Tools;

using Microsoft.Extensions.Options;

using ModelContextProtocol;

var builder = WebApplication.CreateBuilder(args);

builder.AddLogging();
builder.AddComplianceServices();

var mcpOptions = Bind<McpOptions>(builder.Configuration, McpOptions.SectionName);
var coreOptions = Bind<McpCoreOptions>(builder.Configuration, McpCoreOptions.SectionName);
var oauthOptions = Bind<OAuthOptions>(builder.Configuration, OAuthOptions.SectionName);
var rateOptions = Bind<RateLimiterOptions>(builder.Configuration, RateLimiterOptions.RateLimitOptionsSectionName);

builder.Services.AddSingleton<IOptions<McpOptions>>(Options.Create(mcpOptions));
builder.Services.AddSingleton<IOptions<McpCoreOptions>>(Options.Create(coreOptions));
builder.Services.AddSingleton(oauthOptions);
builder.Services.AddSingleton<IOptions<RateLimiterOptions>>(Options.Create(rateOptions));

builder.Services
  .AddErrorHandling()
  .AddHealthChecksServices()
  .AddOAuth(oauthOptions, coreOptions)
  .ConfigureRateLimiter(rateOptions)
  .AddMcpPrimitiveFilters(builder.Configuration)
  .AddMcp(mcpOptions, coreOptions, mcp =>
  {
    var toolSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    toolSerializerOptions.TypeInfoResolverChain.Add(McpToolsJsonContext.Default);

    mcp.WithTools<RandomNumberTools>(toolSerializerOptions)
       .WithResources<DemoResources>()
       .WithPrompts<DemoPrompts>();
  });

if (oauthOptions.EmbeddedOAuthServer is { Enabled: true })
{
  builder.Services.AddHostedService<EmbeddedOAuthServerHostedService>();
}

var app = builder.Build();

var transport = mcpOptions.Transport;
var isHttp = transport is "http" or "both";
var isStdio = transport is "stdio" or "both";

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

static T Bind<T>(IConfiguration configuration, string sectionName) where T : class, new()
{
  var obj = new T();
  configuration.GetSection(sectionName).Bind(obj);
  return obj;
}