using McpServer.Tools;
using Microsoft.Net.Http.Headers;

namespace McpServer.Infrastructure;
public static partial class ApiBuilder
{
  public static IServiceCollection AddMcp(this IServiceCollection services, IConfiguration configuration)
  {
    services
      .AddMcpServer()
      .WithHttpTransport(opts => opts.Stateless = true)
      .WithTools<RandomNumberTools>();

    // If OAuth is not already configured (no OAuthMarker registered),
    // add a basic CORS policy for browser-based MCP clients.
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
      // No OAuth: still need CORS middleware for browser-based
      // MCP clients (Inspector, Claude Desktop, etc.)
      app.UseCors();
      endpoint.RequireCors(McpCorsPolicyName);
    }

    if (app.Services.IsRateLimiterConfigured())
    {
      endpoint.RequireRateLimiting(RateLimiterPolicyNames.McpRateLimits);
    }

    return app;
  }
}