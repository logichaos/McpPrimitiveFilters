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
    var builder = services
      .AddMcpServer(options =>
      {
        options.Capabilities = new ServerCapabilities
        {
          Logging = new LoggingCapability()
        };
      })
      .WithHttpTransport(opts => opts.Stateless = true);

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
    if (!oauthConfigured)
    {
      var allowedOrigins = configuration
          .GetSection("Mcp:AllowedOrigins")
          .Get<string[]>() ?? ["http://localhost:5173", "http://localhost:6274"];

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