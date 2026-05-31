using McpServer.Infrastructure;
using McpServer.Infrastructure.OAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.Unit.Tests.OAuth;

public class ApiBuilderOAuthTests
{
    [Test]
    public async Task NoConfigSection_NoOAuthMarkerRegistered()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddOAuth(config);
        var provider = services.BuildServiceProvider();

        await Assert.That(provider.IsOAuthConfigured()).IsFalse();
    }

    [Test]
    public async Task NoConfigSection_NoAuthenticationServicesRegistered()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddOAuth(config);

        var provider = services.BuildServiceProvider();
        var authOptions = provider.GetService<IOptions<AuthenticationOptions>>();
        await Assert.That(authOptions).IsNull();
    }

    [Test]
    public async Task AllSchemesDisabled_OAuthMarkerNotRegistered()
    {
        var services = new ServiceCollection();
        var config = BuildBaseConfig("InMemory", enabled: false);

        services.AddOAuth(config);
        var provider = services.BuildServiceProvider();

        await Assert.That(provider.IsOAuthConfigured()).IsFalse();
    }

    [Test]
    public async Task EmptySchemes_OAuthMarkerNotRegistered()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:OAuth:DefaultScheme"] = "InMemory",
                ["Mcp:OAuth:ServerUrl"] = "http://localhost:7071/",
            })
            .Build();

        services.AddOAuth(config);
        var provider = services.BuildServiceProvider();

        await Assert.That(provider.IsOAuthConfigured()).IsFalse();
    }

    [Test]
    public async Task SingleSchemeEnabled_OAuthMarkerIsRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildBaseConfig("InMemory", enabled: true);

        services.AddOAuth(config);
        var provider = services.BuildServiceProvider();

        await Assert.That(provider.IsOAuthConfigured()).IsTrue();
    }

    [Test]
    public async Task SingleSchemeEnabled_RegistersAuthentication()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildBaseConfig("InMemory", enabled: true);

        services.AddOAuth(config);
        var provider = services.BuildServiceProvider();

        var authOptions = provider.GetService<IOptions<AuthenticationOptions>>();
        await Assert.That(authOptions).IsNotNull();
    }

    [Test]
    public async Task SingleSchemeEnabled_SetsDefaultAuthenticateScheme()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildBaseConfig("InMemory", enabled: true);

        services.AddOAuth(config);
        var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<IOptions<AuthenticationOptions>>();

        await Assert.That(authOptions.Value.DefaultAuthenticateScheme)
            .IsEqualTo("InMemory");
    }

    [Test]
    public async Task SingleSchemeEnabled_SetsDefaultChallengeScheme()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildBaseConfig("InMemory", enabled: true);

        services.AddOAuth(config);
        var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<IOptions<AuthenticationOptions>>();

        await Assert.That(authOptions.Value.DefaultChallengeScheme)
            .IsEqualTo("McpAuth");
    }

    [Test]
    public async Task SingleSchemeEnabled_AuthorizationServiceIsRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildBaseConfig("InMemory", enabled: true);

        services.AddOAuth(config);
        var provider = services.BuildServiceProvider();

        var authzService = provider.GetService(
            typeof(Microsoft.AspNetCore.Authorization.IAuthorizationService));
        await Assert.That(authzService).IsNotNull();
    }

    [Test]
    public async Task EntraIdSchemeEnabled_OAuthMarkerIsRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildBaseConfig("EntraId", enabled: true);

        services.AddOAuth(config);
        var provider = services.BuildServiceProvider();

        await Assert.That(provider.IsOAuthConfigured()).IsTrue();
    }

    [Test]
    public async Task Auth0SchemeEnabled_OAuthMarkerIsRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildBaseConfig("Auth0", enabled: true);

        services.AddOAuth(config);
        var provider = services.BuildServiceProvider();

        await Assert.That(provider.IsOAuthConfigured()).IsTrue();
    }

    [Test]
    public async Task MultipleSchemesEnabled_UsesPolicyScheme()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:OAuth:DefaultScheme"] = "EntraId",
                ["Mcp:OAuth:ServerUrl"] = "http://localhost:7071/",
                ["Mcp:OAuth:Schemes:InMemory:Enabled"] = "true",
                ["Mcp:OAuth:Schemes:InMemory:Type"] = "InMemory",
                ["Mcp:OAuth:Schemes:InMemory:AuthorityUrl"] = "https://localhost:7029",
                ["Mcp:OAuth:Schemes:EntraId:Enabled"] = "true",
                ["Mcp:OAuth:Schemes:EntraId:Type"] = "EntraId",
                ["Mcp:OAuth:Schemes:EntraId:TenantId"] = "my-tenant",
            })
            .Build();

        services.AddOAuth(config);
        var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<IOptions<AuthenticationOptions>>();

        await Assert.That(authOptions.Value.DefaultAuthenticateScheme)
            .IsEqualTo("MultiScheme");
    }

    [Test]
    public async Task MissingDefaultScheme_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:OAuth:Schemes:InMemory:Enabled"] = "true",
                ["Mcp:OAuth:Schemes:InMemory:Type"] = "InMemory",
                ["Mcp:OAuth:Schemes:InMemory:AuthorityUrl"] = "https://localhost:7029",
            })
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
        {
            services.AddOAuth(config);
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task DefaultSchemeNotEnabled_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:OAuth:DefaultScheme"] = "NonExistent",
                ["Mcp:OAuth:Schemes:InMemory:Enabled"] = "true",
                ["Mcp:OAuth:Schemes:InMemory:Type"] = "InMemory",
                ["Mcp:OAuth:Schemes:InMemory:AuthorityUrl"] = "https://localhost:7029",
            })
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
        {
            services.AddOAuth(config);
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task UnknownProviderType_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:OAuth:DefaultScheme"] = "MyScheme",
                ["Mcp:OAuth:Schemes:MyScheme:Enabled"] = "true",
                ["Mcp:OAuth:Schemes:MyScheme:Type"] = "UnknownProvider",
            })
            .Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
        {
            services.AddOAuth(config);
            return Task.CompletedTask;
        });
    }

    private static IConfiguration BuildBaseConfig(string schemeName, bool enabled)
    {
        var data = new Dictionary<string, string?>
        {
            [$"Mcp:OAuth:DefaultScheme"] = schemeName,
            [$"Mcp:OAuth:ServerUrl"] = "http://localhost:7071/",
            [$"Mcp:OAuth:Schemes:{schemeName}:Enabled"] = enabled.ToString().ToLowerInvariant(),
            [$"Mcp:OAuth:Schemes:{schemeName}:Type"] = schemeName,
        };

        if (schemeName == "InMemory")
            data[$"Mcp:OAuth:Schemes:{schemeName}:AuthorityUrl"] = "https://localhost:7029";
        else if (schemeName == "EntraId")
            data[$"Mcp:OAuth:Schemes:{schemeName}:TenantId"] = "test-tenant";
        else if (schemeName == "Auth0")
            data[$"Mcp:OAuth:Schemes:{schemeName}:Domain"] = "test.auth0.com";

        return new ConfigurationBuilder()
            .AddInMemoryCollection(data)
            .Build();
    }
}
