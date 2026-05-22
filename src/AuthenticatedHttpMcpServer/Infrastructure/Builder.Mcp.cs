using AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;

namespace AuthenticatedHttpMcpServer.Infrastructure;

public static partial class ApiBuilder
{
  public static IServiceCollection AddMcp(this IServiceCollection services)
  {
    services.AddSingleton<McpToolRegistry>();
    // here is where you could decide which tool selection strategy to use
    services.AddSingleton<ToolSelectionStrategy, ScopeToolsClaimsPrincipalToolSelectionStrategy>();
    services.AddSingleton<ToolSelectionStrategy, ToolsOptionsToolSelectionStrategy>();

    services
      .AddMcpServer()
      .AddAuthorizationFilters()
      .WithToolsFromAssembly(typeof(ApiBuilder).Assembly)
      .WithHttpTransport(opts =>
      {
        opts.Stateless = false;

        opts.ConfigureSessionOptions = (ctx, mcpOpts, _) =>
        {
          McpToolRegistry registry = ctx.RequestServices.GetRequiredService<McpToolRegistry>();
          IEnumerable<ToolSelectionStrategy> toolSelectionStrategies =
            ctx.RequestServices.GetRequiredService<IEnumerable<ToolSelectionStrategy>>();

          mcpOpts.ToolCollection = [..registry.FilterToolsUsingStrategy(toolSelectionStrategies)];

          return Task.CompletedTask;
        };
      });

    return services;
  }
}