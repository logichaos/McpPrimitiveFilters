using McpServer.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.Unit.Tests;

public class ApiBuilderMcpTests
{
    [Test]
    public async Task AddMcp_WithoutOAuth_AddsCorsPolicy_WithDefaultOrigins()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder().Build();

        services.AddMcp(config);
        var provider = services.BuildServiceProvider();

        var corsOpts = provider.GetService<IOptions<CorsOptions>>();
        await Assert.That(corsOpts).IsNotNull();

        var policy = corsOpts!.Value.GetPolicy(McpCorsPolicyName);
        await Assert.That(policy).IsNotNull();
        await Assert.That(policy!.Origins)
            .Contains("http://localhost:5173")
            .And.Contains("http://localhost:6274");
    }

    [Test]
    public async Task AddMcp_WithoutOAuth_AllowsExpectedMethods()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder().Build();

        services.AddMcp(config);
        var provider = services.BuildServiceProvider();
        var corsOpts = provider.GetRequiredService<IOptions<CorsOptions>>();
        var policy = corsOpts.Value.GetPolicy(McpCorsPolicyName)!;

        await Assert.That(policy.Methods)
            .Contains("POST")
            .And.Contains("GET")
            .And.Contains("DELETE")
            .And.Contains("OPTIONS");
    }

    [Test]
    public async Task AddMcp_WithoutOAuth_ExposesMcpSessionIdHeader()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder().Build();

        services.AddMcp(config);
        var provider = services.BuildServiceProvider();
        var corsOpts = provider.GetRequiredService<IOptions<CorsOptions>>();
        var policy = corsOpts.Value.GetPolicy(McpCorsPolicyName)!;

        await Assert.That(policy.ExposedHeaders)
            .Contains("Mcp-Session-Id");
    }

    [Test]
    public async Task AddMcp_WithoutOAuth_UsesCustomAllowedOrigins_FromConfig()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:AllowedOrigins:0"] = "https://custom.example.com",
                ["Mcp:AllowedOrigins:1"] = "https://other.example.com",
            })
            .Build();

        services.AddMcp(config);
        var provider = services.BuildServiceProvider();
        var corsOpts = provider.GetRequiredService<IOptions<CorsOptions>>();
        var policy = corsOpts.Value.GetPolicy(McpCorsPolicyName)!;

        await Assert.That(policy.Origins)
            .Contains("https://custom.example.com")
            .And.Contains("https://other.example.com");
        await Assert.That(policy.Origins)
            .DoesNotContain("http://localhost:5173");
    }

    [Test]
    public async Task AddMcp_WithOAuthMarker_SkipsCorsConfiguration()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new ApiBuilder.OAuthMarker());
        var config = new ConfigurationBuilder().Build();

        services.AddMcp(config);
        var provider = services.BuildServiceProvider();

        var corsOpts = provider.GetService<IOptions<CorsOptions>>();
        if (corsOpts is not null)
        {
            await Assert.That(corsOpts.Value.GetPolicy(McpCorsPolicyName)).IsNull();
        }
    }

    [Test]
    public async Task AddMcp_AlwaysRegistersMcpServer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder().Build();

        services.AddMcp(config);

        var mcpDescriptors = services
            .Where(sd => sd.ServiceType.FullName?.Contains("Mcp") == true
                      || sd.ServiceType.Namespace?.StartsWith("ModelContextProtocol") == true)
            .ToList();

        await Assert.That(mcpDescriptors.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task AddMcp_WithOAuthMarker_StillRegistersMcpServer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new ApiBuilder.OAuthMarker());
        var config = new ConfigurationBuilder().Build();

        services.AddMcp(config);

        var mcpDescriptors = services
            .Where(sd => sd.ServiceType.FullName?.Contains("Mcp") == true
                      || sd.ServiceType.Namespace?.StartsWith("ModelContextProtocol") == true)
            .ToList();

        await Assert.That(mcpDescriptors.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task UseMcp_MapsEndpoint()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.Services.AddMcpServer().WithHttpTransport(opts => opts.Stateless = true);
        var app = builder.Build();

        ApiBuilder.UseMcp(app);

        var endpoints = GetEndpoints(app);
        await Assert.That(endpoints.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task UseMcp_WithoutOAuth_UsesCorsOnEndpoint()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.Services.AddMcpServer().WithHttpTransport(opts => opts.Stateless = true);
        builder.Services.AddCors();
        var app = builder.Build();

        ApiBuilder.UseMcp(app);

        var endpoint = GetEndpoints(app)[0];
        var corsMeta = endpoint.Metadata
            .GetMetadata<Microsoft.AspNetCore.Cors.Infrastructure.IEnableCorsAttribute>();
        await Assert.That(corsMeta).IsNotNull();
    }

    [Test]
    public async Task UseMcp_WithOAuth_RequiresAuthorization()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.Services.AddMcpServer().WithHttpTransport(opts => opts.Stateless = true);
        builder.Services.AddSingleton(new ApiBuilder.OAuthMarker());
        builder.Services.AddAuthorization();
        builder.Services.AddCors();
        var app = builder.Build();

        ApiBuilder.UseMcp(app);

        var endpoint = GetEndpoints(app)[0];
        var authData = endpoint.Metadata.GetMetadata<IAuthorizeData>();
        await Assert.That(authData).IsNotNull();
    }

    [Test]
    public async Task UseMcp_WithRateLimiter_RequiresMcpRateLimiting()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.Services.AddMcpServer().WithHttpTransport(opts => opts.Stateless = true);
        builder.Services.AddSingleton(new ApiBuilder.RateLimiterMarker());
        builder.Services.AddRateLimiter(opt =>
        {
            opt.AddFixedWindowLimiter(RateLimiterPolicyNames.McpRateLimits, o =>
            {
                o.PermitLimit = 10;
                o.Window = TimeSpan.FromMinutes(1);
            });
        });
        var app = builder.Build();

        ApiBuilder.UseMcp(app);

        var endpoint = GetEndpoints(app)[0];
        var rateLimitMeta = endpoint.Metadata
            .GetMetadata<EnableRateLimitingAttribute>();
        await Assert.That(rateLimitMeta).IsNotNull();
        await Assert.That(rateLimitMeta!.PolicyName)
            .IsEqualTo(RateLimiterPolicyNames.McpRateLimits);
    }

    [Test]
    public async Task UseMcp_WithoutRateLimiter_NoRateLimiting()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.Services.AddMcpServer().WithHttpTransport(opts => opts.Stateless = true);
        var app = builder.Build();

        ApiBuilder.UseMcp(app);

        var endpoint = GetEndpoints(app)[0];
        var rateLimitMeta = endpoint.Metadata
            .GetMetadata<EnableRateLimitingAttribute>();
        await Assert.That(rateLimitMeta).IsNull();
    }

    private static IReadOnlyList<Endpoint> GetEndpoints(WebApplication app)
    {
        var diSource = app.Services.GetService<EndpointDataSource>();
        var diEndpoints = diSource?.Endpoints ?? [];
        var rbEndpoints = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(ds => ds.Endpoints);
        return diEndpoints.Concat(rbEndpoints).Distinct().ToList();
    }

    private const string McpCorsPolicyName = "McpBrowserClient";
}
