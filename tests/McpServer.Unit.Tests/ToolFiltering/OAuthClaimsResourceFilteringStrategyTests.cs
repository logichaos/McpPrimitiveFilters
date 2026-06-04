using System.Security.Claims;
using McpServer.Infrastructure.ToolFiltering;
using Microsoft.AspNetCore.Http;

namespace McpServer.Unit.Tests.ToolFiltering;

public class OAuthClaimsResourceFilteringStrategyTests
{
    private readonly OAuthClaimsResourceFilteringStrategy _strategy = new();

    private static DefaultHttpContext CreateHttpContext(ClaimsPrincipal? user = null)
    {
        return new DefaultHttpContext { User = user ?? new ClaimsPrincipal() };
    }

    [Test]
    public async Task AuthenticatedUser_WithMatchingClaims_ReturnsOnlyClaimedResources()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("scope", "mcp.resource.Server Info"),
            new Claim("scope", "mcp.resource.Process Info"),
        ], "test"));

        var httpContext = CreateHttpContext(principal);
        var resourceNames = new[] { "Server Info", "Process Info", "Current Time" };

        var result = _strategy.FilterResources(httpContext, resourceNames).ToList();

        await Assert.That(result).Count().IsEqualTo(2);
        await Assert.That(result).Contains("Server Info");
        await Assert.That(result).Contains("Process Info");
        await Assert.That(result).DoesNotContain("Current Time");
    }

    [Test]
    public async Task AuthenticatedUser_WithResourcesAllScope_ReturnsAllResourcesUnchanged()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("scope", "mcp.resources.all"),
        ], "test"));

        var httpContext = CreateHttpContext(principal);
        var resourceNames = new[] { "Server Info", "City Weather", "Process Info", "Current Time" };

        var result = _strategy.FilterResources(httpContext, resourceNames).ToList();

        await Assert.That(result).Count().IsEqualTo(4);
        await Assert.That(result).Contains("Server Info");
        await Assert.That(result).Contains("City Weather");
        await Assert.That(result).Contains("Process Info");
        await Assert.That(result).Contains("Current Time");
    }

    [Test]
    public async Task AuthenticatedUser_WithNoResourceClaims_ReturnsEmptyList()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", "user123"),
            new Claim("email", "user@example.com"),
        ], "test"));

        var httpContext = CreateHttpContext(principal);
        var resourceNames = new[] { "Server Info", "Process Info", "Current Time" };

        var result = _strategy.FilterResources(httpContext, resourceNames).ToList();

        await Assert.That(result).Count().IsEqualTo(0);
    }

    [Test]
    public async Task UnauthenticatedUser_ReturnsAllResourcesUnchanged()
    {
        var httpContext = CreateHttpContext();
        var resourceNames = new[] { "Server Info", "Process Info", "Current Time" };

        var result = _strategy.FilterResources(httpContext, resourceNames).ToList();

        await Assert.That(result).Count().IsEqualTo(3);
        await Assert.That(result).Contains("Server Info");
        await Assert.That(result).Contains("Process Info");
        await Assert.That(result).Contains("Current Time");
    }
}
