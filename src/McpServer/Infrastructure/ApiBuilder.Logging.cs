namespace McpServer.Infrastructure;

public static partial class ApiBuilder
{
    public static WebApplicationBuilder AddLogging(this WebApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        return builder;
    }

    public static WebApplication UseLogging(this WebApplication app)
    {
        return app;
    }
}
