using AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;
using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer.Infrastructure;

public static partial class ApiBuilder
{
  public static IServiceCollection AddMcp(this IServiceCollection services)
  {
    services.AddSingleton<McpToolRegistry>();
    services.AddSingleton<HttpContextToolSelectionStrategy, ScopeToolsClaimsPrincipalToolSelectionStrategy>();

    services
      .AddMcpServer()
      .AddAuthorizationFilters()
      .WithHttpTransport(opts =>
      {
        opts.Stateless = false;

        opts.ConfigureSessionOptions = (ctx, mcpOpts, _) =>
        {
          McpToolRegistry registry = ctx.RequestServices.GetRequiredService<McpToolRegistry>();
          HttpContextToolSelectionStrategy toolSelectionStrategy =
            ctx.RequestServices.GetRequiredService<HttpContextToolSelectionStrategy>();
          IEnumerable<McpServerTool> userTools = registry.GetToolsForClaimsPrincipal(ctx, toolSelectionStrategy);
          mcpOpts.ToolCollection = [.. userTools];

          return Task.CompletedTask;
        };
      });

    return services;
  }
}