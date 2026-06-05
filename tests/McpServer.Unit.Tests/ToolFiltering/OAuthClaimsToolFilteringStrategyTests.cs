using System.Security.Claims;
using McpServer.Infrastructure.ToolFiltering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Unit.Tests.ToolFiltering;

public class OAuthClaimsToolFilteringStrategyTests
{
    private readonly OAuthClaimsToolFilteringStrategy _strategy = new(
        NullLogger<OAuthClaimsToolFilteringStrategy>.Instance);

    private static DefaultHttpContext CreateHttpContext(ClaimsPrincipal? user = null)
    {
        return new DefaultHttpContext { User = user ?? new ClaimsPrincipal() };
    }

    [Test]
    public async Task AuthenticatedUser_WithMatchingScopeClaims_ReturnsOnlyClaimedTools()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("scope", "mcp.tool.GetRandomNumber"),
            new Claim("scope", "mcp.tool.Echo"),
        ], "test"));

        var httpContext = CreateHttpContext(principal);
        var toolNames = new[] { "GetRandomNumber", "Echo", "GetTimestamp" };

        var result = _strategy.FilterTools(httpContext, toolNames).ToList();

        await Assert.That(result).Count().IsEqualTo(2);
        await Assert.That(result).Contains("GetRandomNumber");
        await Assert.That(result).Contains("Echo");
        await Assert.That(result).DoesNotContain("GetTimestamp");
    }

    [Test]
    public async Task AuthenticatedUser_WithToolsAllScope_ReturnsAllTools()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("scope", "mcp.tools.all"),
        ], "test"));

        var httpContext = CreateHttpContext(principal);
        var toolNames = new[] { "GetRandomNumber", "Echo", "GetTimestamp" };

        var result = _strategy.FilterTools(httpContext, toolNames).ToList();

        await Assert.That(result).Count().IsEqualTo(3);
    }

    [Test]
    public async Task AuthenticatedUser_WithNoToolScopes_ReturnsEmptyList()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", "user123"),
            new Claim("email", "user@example.com"),
        ], "test"));

        var httpContext = CreateHttpContext(principal);
        var toolNames = new[] { "GetRandomNumber", "Echo", "GetTimestamp" };

        var result = _strategy.FilterTools(httpContext, toolNames).ToList();

        await Assert.That(result).Count().IsEqualTo(0);
    }

    [Test]
    public async Task UnauthenticatedUser_ReturnsAllToolsUnchanged()
    {
        var httpContext = CreateHttpContext();
        var toolNames = new[] { "GetRandomNumber", "Echo", "GetTimestamp" };

        var result = _strategy.FilterTools(httpContext, toolNames).ToList();

        await Assert.That(result).Count().IsEqualTo(3);
        await Assert.That(result).Contains("GetRandomNumber");
        await Assert.That(result).Contains("Echo");
        await Assert.That(result).Contains("GetTimestamp");
    }

    [Test]
    public async Task AuthenticatedUser_NonMatchingScopeValues_ExcludesTools()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("scope", "mcp.tool.Echo"),
        ], "test"));

        var httpContext = CreateHttpContext(principal);
        var toolNames = new[] { "Echo", "GetRandomNumber" };

        var result = _strategy.FilterTools(httpContext, toolNames).ToList();

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result).Contains("Echo");
        await Assert.That(result).DoesNotContain("GetRandomNumber");
    }
}
