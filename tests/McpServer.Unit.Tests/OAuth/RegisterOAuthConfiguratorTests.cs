using McpServer.Infrastructure;
using McpServer.Infrastructure.OAuth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace McpServer.Unit.Tests.OAuth;

public class RegisterOAuthConfiguratorTests
{
    [Test]
    public async Task CustomConfigurator_CanBeRegisteredAndUsed()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        ApiBuilder.RegisterOAuthConfigurator(new CustomTestConfigurator());

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:OAuth:DefaultScheme"] = "CustomScheme",
                ["Mcp:OAuth:ServerUrl"] = "http://localhost:7071/",
                ["Mcp:OAuth:Schemes:CustomScheme:Enabled"] = "true",
                ["Mcp:OAuth:Schemes:CustomScheme:Type"] = "CustomTest",
            })
            .Build();

        services.AddOAuth(config);
        var provider = services.BuildServiceProvider();

        await Assert.That(provider.IsOAuthConfigured()).IsTrue();
    }

    [Test]
    public async Task CustomConfigurator_OverwritesExistingType()
    {
        var custom = new CustomTestConfigurator { ProviderTypeOverride = "InMemory" };
        ApiBuilder.RegisterOAuthConfigurator(custom);

        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:OAuth:DefaultScheme"] = "InMemory",
                ["Mcp:OAuth:ServerUrl"] = "http://localhost:7071/",
                ["Mcp:OAuth:Schemes:InMemory:Enabled"] = "true",
                ["Mcp:OAuth:Schemes:InMemory:Type"] = "InMemory",
            })
            .Build();

        services.AddOAuth(config);
        var provider = services.BuildServiceProvider();

        await Assert.That(provider.IsOAuthConfigured()).IsTrue();
    }

    private sealed class CustomTestConfigurator : OAuthSchemeConfigurator
    {
        public string ProviderTypeOverride { get; set; } = "CustomTest";

        public string ProviderType => ProviderTypeOverride;

        public void Configure(JwtBearerOptions options, OAuthSchemeConfig scheme, OAuthOptions oauth, ILoggerFactory loggerFactory)
        {
            options.Authority = "https://custom.example.com/";
        }
    }
}
