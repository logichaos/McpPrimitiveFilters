using System.Security.Claims;
using McpServer.Infrastructure.ToolFiltering;
using Microsoft.AspNetCore.Http;

namespace McpServer.Unit.Tests.ToolFiltering;

public class OAuthClaimsToolFilteringStrategyTests
{
    private readonly OAuthClaimsToolFilteringStrategy _strategy = new();

    private static DefaultHttpContext CreateHttpContext(ClaimsPrincipal? user = null)
    {
        return new DefaultHttpContext { User = user ?? new ClaimsPrincipal() };
    }

    // ── Authenticated user with matching claims ──

    [Test]
    public async Task AuthenticatedUser_WithMatchingClaims_ReturnsOnlyClaimedTools()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("mcp.tool.GetRandomNumber", "true"),
            new Claim("mcp.tool.Echo", "true"),
        ], "test"));

        var httpContext = CreateHttpContext(principal);
        var toolNames = new[] { "GetRandomNumber", "Echo", "GetTimestamp" };

        var result = _strategy.FilterTools(httpContext, toolNames).ToList();

        await Assert.That(result).Count().IsEqualTo(2);
        await Assert.That(result).Contains("GetRandomNumber");
        await Assert.That(result).Contains("Echo");
        await Assert.That(result).DoesNotContain("GetTimestamp");
    }

    // ── Authenticated user with no tool claims ──

    [Test]
    public async Task AuthenticatedUser_WithNoToolClaims_ReturnsEmptyList()
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

    // ── Unauthenticated user ──

    [Test]
    public async Task UnauthenticatedUser_ReturnsAllToolsUnchanged()
    {
        var httpContext = CreateHttpContext(); // no authenticated user
        var toolNames = new[] { "GetRandomNumber", "Echo", "GetTimestamp" };

        var result = _strategy.FilterTools(httpContext, toolNames).ToList();

        await Assert.That(result).Count().IsEqualTo(3);
        await Assert.That(result).Contains("GetRandomNumber");
        await Assert.That(result).Contains("Echo");
        await Assert.That(result).Contains("GetTimestamp");
    }

    // ── Claim value is not "true" ──

    [Test]
    public async Task AuthenticatedUser_ClaimValueIsFalse_ToolIsExcluded()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("mcp.tool.Echo", "false"),
            new Claim("mcp.tool.GetRandomNumber", "true"),
        ], "test"));

        var httpContext = CreateHttpContext(principal);
        var toolNames = new[] { "Echo", "GetRandomNumber" };

        var result = _strategy.FilterTools(httpContext, toolNames).ToList();

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result).Contains("GetRandomNumber");
        await Assert.That(result).DoesNotContain("Echo");
    }

    // ── Claim value case sensitivity ──

    [Test]
    public async Task AuthenticatedUser_ClaimValueIsTrue_IsCaseSensitive()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("mcp.tool.Echo", "TRUE"), // uppercase — should not match
        ], "test"));

        var httpContext = CreateHttpContext(principal);
        var toolNames = new[] { "Echo" };

        var result = _strategy.FilterTools(httpContext, toolNames).ToList();

        await Assert.That(result).Count().IsEqualTo(0);
    }
}
