using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace McpServer.Infrastructure;

public static partial class ApiBuilder
{
  public static IServiceCollection AddMcp(
      this IServiceCollection services,
      IConfiguration configuration,
      Action<IMcpServerBuilder>? configureMcp = null)
  {
    var mcp = BindFromConfig<McpOptions>(configuration, McpOptions.SectionName);
    var core = BindFromConfig<McpCoreOptions>(configuration, McpCoreOptions.SectionName);
    return services.AddMcp(mcp, core, configureMcp);
  }

  public static IServiceCollection AddMcp(
      this IServiceCollection services,
      McpOptions transportOptions,
      McpCoreOptions coreOptions,
      Action<IMcpServerBuilder>? configureMcp = null)
  {
    var transport = transportOptions.Transport;
    var isHttp = transport is "http" or "both";
    var isStdio = transport is "stdio" or "both";

    var builder = services
      .AddMcpServer(options =>
      {
        options.Capabilities = new ServerCapabilities
        {
          Logging = new LoggingCapability(),
          Prompts = new PromptsCapability { ListChanged = false }
        };
      });

    if (isHttp)
    {
      builder.WithHttpTransport(opts => opts.Stateless = true);
    }

    if (isStdio)
    {
      builder.WithStdioServerTransport();
    }

    configureMcp?.Invoke(builder);

    builder
      .WithSetLoggingLevelHandler(async (ctx, ct) =>
      {
        if (ctx.Params?.Level is null)
        {
          throw new McpProtocolException(
            "Missing required argument 'level'",
            McpErrorCode.InvalidParams);
        }

        var levelService = ctx.Services?.GetRequiredService<DynamicLogLevelService>();
        if (levelService is not null)
        {
          levelService.MinLevel = DynamicLogLevelService.MapMCPLevelToNetLevel(ctx.Params.Level);
        }

        return new EmptyResult();
      });

    var oauthConfigured = services.Any(sd => sd.ServiceType == typeof(OAuthMarker));
    if (!oauthConfigured && isHttp)
    {
      var allowedOrigins = coreOptions.AllowedOrigins.Length > 0
          ? coreOptions.AllowedOrigins
          : new[] { "http://localhost:5173", "http://localhost:6274" };

      services.AddCors(options =>
      {
        options.AddPolicy(McpCorsPolicyName, policy =>
        {
          policy.WithOrigins(allowedOrigins)
              .WithMethods("POST", "GET", "DELETE", "OPTIONS")
              .WithHeaders(HeaderNames.ContentType, HeaderNames.Authorization, "MCP-Protocol-Version")
              .WithExposedHeaders("Mcp-Session-Id");
        });
      });
    }

    return services;
  }

  public static WebApplication UseMcp(this WebApplication app)
  {
    var transportOptions = app.Services.GetRequiredService<IOptions<McpOptions>>().Value;
    var transport = transportOptions.Transport;
    var isHttp = transport is "http" or "both";

    if (!isHttp)
    {
      return app;
    }

    var endpoint = app.MapMcp();

    if (app.Services.IsOAuthConfigured())
    {
      endpoint.RequireAuthorization();
      endpoint.RequireCors(McpCorsPolicyName);
    }
    else
    {
      app.UseCors(McpCorsPolicyName);
      endpoint.RequireCors(McpCorsPolicyName);
    }

    if (app.Services.IsRateLimiterConfigured())
    {
      endpoint.RequireRateLimiting(RateLimiterPolicyNames.McpRateLimits);
    }

    return app;
  }
}