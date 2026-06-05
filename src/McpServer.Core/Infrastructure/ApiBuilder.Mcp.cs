using McpServer.Infrastructure.ToolFiltering;

using Microsoft.Net.Http.Headers;

using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

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
          var logger = context.Services?.GetService<ILoggerFactory>()?.CreateLogger("McpServer.ToolFilter");

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
              ToolFilteringLogMessages.CallDenied(logger!, httpContext.User.Identity?.Name, toolName);
              return new CallToolResult
              {
                Content = [new TextContentBlock { Text = $"Tool '{toolName}' is not authorized." }],
                IsError = true
              };
            }
          }

          return await next(context, cancellationToken);
        });

        filters.AddListResourcesFilter(next => async (context, cancellationToken) =>
        {
          var result = await next(context, cancellationToken);

          if (result.Resources is { Count: > 0 })
          {
            var httpContextAccessor = context.Services?.GetService<IHttpContextAccessor>();
            var strategies = context.Services?.GetServices<ResourceFilteringStrategy>();

            if (httpContextAccessor?.HttpContext is { } httpContext && strategies is not null)
            {
              var resourceNames = result.Resources.Select(r => r.Name).ToList();
              foreach (var strategy in strategies)
              {
                resourceNames = strategy.FilterResources(httpContext, resourceNames).ToList();
              }

              var allowedNames = new HashSet<string>(resourceNames, StringComparer.OrdinalIgnoreCase);
              result.Resources = result.Resources.Where(r => allowedNames.Contains(r.Name)).ToList();
            }
          }

          return result;
        });

        filters.AddListResourceTemplatesFilter(next => async (context, cancellationToken) =>
        {
          var result = await next(context, cancellationToken);

          if (result.ResourceTemplates is { Count: > 0 })
          {
            var httpContextAccessor = context.Services?.GetService<IHttpContextAccessor>();
            var strategies = context.Services?.GetServices<ResourceFilteringStrategy>();

            if (httpContextAccessor?.HttpContext is { } httpContext && strategies is not null)
            {
              var resourceNames = result.ResourceTemplates.Select(r => r.Name).ToList();
              foreach (var strategy in strategies)
              {
                resourceNames = strategy.FilterResources(httpContext, resourceNames).ToList();
              }

              var allowedNames = new HashSet<string>(resourceNames, StringComparer.OrdinalIgnoreCase);
              result.ResourceTemplates = result.ResourceTemplates.Where(r => allowedNames.Contains(r.Name)).ToList();
            }
          }

          return result;
        });

        filters.AddReadResourceFilter(next => async (context, cancellationToken) =>
        {
          var httpContextAccessor = context.Services?.GetService<IHttpContextAccessor>();
          var strategies = context.Services?.GetServices<ResourceFilteringStrategy>();
          var logger = context.Services?.GetService<ILoggerFactory>()?.CreateLogger("McpServer.ResourceFilter");

          if (httpContextAccessor?.HttpContext is { } httpContext
              && strategies is not null
              && context.Params?.Uri is { } uri)
          {
            var serverResources = context.Services?.GetServices<McpServerResource>();
            if (serverResources is not null)
            {
              foreach (var resource in serverResources)
              {
                if (resource.IsMatch(uri))
                {
                  var resourceName = resource.ProtocolResource?.Name
                    ?? resource.ProtocolResourceTemplate?.Name;

                  if (resourceName is not null)
                  {
                    var names = new[] { resourceName }.AsEnumerable();
                    foreach (var strategy in strategies)
                    {
                      names = strategy.FilterResources(httpContext, names);
                    }

                    if (!names.Any())
                    {
                      ResourceFilteringLogMessages.ReadDenied(logger!, httpContext.User.Identity?.Name, resourceName, uri);
                      return new ReadResourceResult
                      {
                        Contents = [new TextResourceContents
                        {
                          Uri = uri,
                          MimeType = "text/plain",
                          Text = $"Resource '{uri}' is not authorized."
                        }]
                      };
                    }
                  }

                  break;
                }
              }
            }
          }

          return await next(context, cancellationToken);
        });
      })
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