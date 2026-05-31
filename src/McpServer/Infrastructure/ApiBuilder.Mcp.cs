using System.Text.Json;
using McpServer.Infrastructure.ToolFiltering;
using McpServer.Tools;
using Microsoft.Net.Http.Headers;
using ModelContextProtocol.Protocol;

namespace McpServer.Infrastructure;
public static partial class ApiBuilder
{
  public static IServiceCollection AddMcp(this IServiceCollection services, IConfiguration configuration)
  {
    var toolSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    toolSerializerOptions.TypeInfoResolverChain.Add(McpToolsJsonContext.Default);

    services
      .AddMcpServer()
      .WithHttpTransport(opts => opts.Stateless = true)
      .WithTools<RandomNumberTools>(toolSerializerOptions)
      .WithRequestFilters(filters =>
      {
        filters.AddListToolsFilter(next => async (context, cancellationToken) =>
        {
          var result = await next(context, cancellationToken);

          if (result.Tools is { Count: > 0 })
          {
            var httpContextAccessor = context.Services?.GetService<IHttpContextAccessor>();
            var strategies = context.Services?.GetServices<ToolFilteringStrategy>();

            if (httpContextAccessor?.HttpContext is { } httpContext && strategies is not null)
            {
              var toolNames = result.Tools.Select(t => t.Name).ToList();
              foreach (var strategy in strategies)
              {
                toolNames = strategy.FilterTools(httpContext, toolNames).ToList();
              }

              var allowedNames = new HashSet<string>(toolNames, StringComparer.OrdinalIgnoreCase);
              result.Tools = result.Tools.Where(t => allowedNames.Contains(t.Name)).ToList();
            }
          }

          return result;
        });

        filters.AddCallToolFilter(next => async (context, cancellationToken) =>
        {
          var httpContextAccessor = context.Services?.GetService<IHttpContextAccessor>();
          var strategies = context.Services?.GetServices<ToolFilteringStrategy>();

          if (httpContextAccessor?.HttpContext is { } httpContext
              && strategies is not null
              && context.Params is { } requestParams)
          {
            var toolName = requestParams.Name;
            var names = new[] { toolName }.AsEnumerable();
            foreach (var strategy in strategies)
            {
              names = strategy.FilterTools(httpContext, names);
            }

            if (!names.Any())
            {
              return new CallToolResult
              {
                Content = [new TextContentBlock { Text = $"Tool '{toolName}' is not authorized." }],
                IsError = true
              };
            }
          }

          return await next(context, cancellationToken);
        });
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