namespace McpServer.Infrastructure;

public static partial class ApiBuilder
{
    public static WebApplicationBuilder AddLogging(this WebApplicationBuilder builder)
    {
        var levelService = new DynamicLogLevelService();
        builder.Services.AddSingleton(levelService);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
        builder.Logging.AddFilter((category, level) => level >= levelService.MinLevel);
        return builder;
    }

    public static WebApplication UseLogging(this WebApplication app)
    {
        return app;
    }
}
