using Serilog;

public static partial class ApiBuilder
{
    public static WebApplicationBuilder AddLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, config) => config.ReadFrom.Configuration(ctx.Configuration));
        return builder;
    }

    public static WebApplication UseLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        return app;
    }
}
