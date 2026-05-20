namespace AuthenticatedHttpMcpServer.Infrastructure;

public static partial class ApiBuilder
{
  public static IHostApplicationBuilder AddLoggingServices(this IHostApplicationBuilder builder)
  {
    builder.Logging
      .AddJsonConsole(opts =>
      {
        opts.IncludeScopes = true;
        opts.TimestampFormat = "HH:mm:ss ";
      })
      .SetMinimumLevel(LogLevel.Trace);

    return builder;
  }
}