namespace AuthenticatedHttpMcpServer.Infrastructure;

public static partial class ApiBuilder
{
  public static IServiceCollection AddMcp(this IServiceCollection services)
  {
    services.AddSingleton<McpToolRegistry>();

    services
      .AddMcpServer()
      .AddAuthorizationFilters()
      .WithHttpTransport(opts =>
      {
        opts.Stateless = false;

        opts.ConfigureSessionOptions = (ctx, mcpOpts, _) =>
        {
          var registry = ctx.RequestServices.GetRequiredService<McpToolRegistry>();
          var userTools = registry.GetToolsForClaimsPrincipal(ctx.User);
          mcpOpts.ToolCollection = [.. userTools];

          return Task.CompletedTask;
        };
      });

    return services;
  }
}