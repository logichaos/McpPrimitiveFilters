using AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TUnit.AspNetCore;
using TUnit.Core.Interfaces;

namespace AuthenticatedHttpMcpServer.Integration.Tests;

public class TestWebApplicationFactory : TestWebApplicationFactory<Program>, IAsyncInitializer
{
    public Task InitializeAsync()
    {
        _ = Server;
        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ToolsSelection:AllowedTools:0"] = "random_number"
            }));
        builder.ConfigureTestServices(services =>
        {
            // AuthenticationOptions throws on duplicate scheme names, so clear existing
            // registrations before adding the test handler under the same "Bearer" scheme.
            // This keeps the MrAwesome policy resolving correctly without touching auth config.
            var existing = services
                .Where(d => d.ServiceType == typeof(IConfigureOptions<AuthenticationOptions>))
                .ToList();
            foreach (var descriptor in existing)
                services.Remove(descriptor);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddScheme<TestAuthHandlerOptions, TestAuthHandler>(
                    JwtBearerDefaults.AuthenticationScheme, _ => { });
        });
    }
}

// AllowedTools = null → strategy passes all tools through.
public class AllToolsWebApplicationFactory : TestWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
            services.PostConfigure<ToolsSelectionOptions>(opts => opts.AllowedTools = null));
    }
}

// AllowedTools = [] → strategy blocks all tools.
public class EmptyAllowedToolsWebApplicationFactory : TestWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
            services.PostConfigure<ToolsSelectionOptions>(opts => opts.AllowedTools = []));
    }
}
