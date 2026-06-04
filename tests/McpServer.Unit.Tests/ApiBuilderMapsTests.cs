using McpServer.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Unit.Tests;

public class ApiBuilderMapsTests
{
    [Test]
    public async Task UseMaps_MapsRootGetEndpoint()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.Services.AddHealthChecksServices();
        var app = builder.Build();

        ApiBuilder_Maps.UseMaps(app);

        var rootEndpoint = GetEndpoints(app)
            .OfType<RouteEndpoint>()
            .FirstOrDefault(e => e.RoutePattern.RawText == "/");

        await Assert.That(rootEndpoint).IsNotNull();

        var httpMethods = rootEndpoint!.Metadata.GetMetadata<HttpMethodMetadata>();
        await Assert.That(httpMethods).IsNotNull();
        await Assert.That(httpMethods!.HttpMethods).Contains("GET");
    }

    [Test]
    public async Task UseMaps_WithRateLimiter_RequiresFixedRateLimiting()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.Services.AddHealthChecksServices();
        builder.Services.AddSingleton(new ApiBuilder.RateLimiterMarker());
        builder.Services.AddRateLimiter(opt =>
        {
            opt.AddFixedWindowLimiter(RateLimiterPolicyNames.Fixed, o =>
            {
                o.PermitLimit = 20;
                o.Window = TimeSpan.FromMinutes(2);
            });
        });
        var app = builder.Build();

        ApiBuilder_Maps.UseMaps(app);

        var rootEndpoint = GetEndpoints(app)
            .OfType<RouteEndpoint>()
            .FirstOrDefault(e => e.RoutePattern.RawText == "/");

        await Assert.That(rootEndpoint).IsNotNull();
        var rateLimitMeta = rootEndpoint!.Metadata
            .GetMetadata<EnableRateLimitingAttribute>();
        await Assert.That(rateLimitMeta).IsNotNull();
        await Assert.That(rateLimitMeta!.PolicyName)
            .IsEqualTo(RateLimiterPolicyNames.Fixed);
    }

    [Test]
    public async Task UseMaps_WithoutRateLimiter_NoRateLimitingOnEndpoint()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
        builder.Services.AddHealthChecksServices();
        var app = builder.Build();

        ApiBuilder_Maps.UseMaps(app);

        var rootEndpoint = GetEndpoints(app)
            .OfType<RouteEndpoint>()
            .FirstOrDefault(e => e.RoutePattern.RawText == "/");

        await Assert.That(rootEndpoint).IsNotNull();
        var rateLimitMeta = rootEndpoint!.Metadata
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
}
