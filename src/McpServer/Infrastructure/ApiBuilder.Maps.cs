namespace McpServer.Infrastructure;

public static partial class ApiBuilder
{
  public static IServiceCollection AddHealthChecksServices(this IServiceCollection services)
  {
    services.AddHealthChecks();
    return services;
  }

  public static WebApplication UseMaps(this WebApplication app)
  {
    var rootEndpoint = app.MapGet("/", () => "this is working");

    app.MapHealthChecks("/health");

    if (app.Services.IsRateLimiterConfigured())
    {
      app.UseRateLimiter();
      rootEndpoint.RequireRateLimiting(RateLimiterPolicyNames.Fixed);
    }

    return app;
  }
}