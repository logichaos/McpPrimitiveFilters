using System.Security.Claims;
using FakeItEasy;
using McpPrimitiveFilters;
using McpPrimitiveFilters.Strategies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpPrimitiveFilters.Unit.Tests;

public class OAuthClaimsFilteringStrategyTests
{
    private static OAuthClaimsFilteringStrategy CreateStrategy(HttpContext? httpContext = null)
    {
        var accessor = A.Fake<IHttpContextAccessor>();
        A.CallTo(() => accessor.HttpContext).Returns(httpContext!);
        return new OAuthClaimsFilteringStrategy(
            accessor, NullLogger<OAuthClaimsFilteringStrategy>.Instance);
    }

    private static DefaultHttpContext CreateHttpContext(ClaimsPrincipal? user = null)
        => new() { User = user ?? new ClaimsPrincipal() };

    [Test]
    public async Task AuthenticatedUser_WithMatchingToolScopes_ReturnsOnlyClaimedTools()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("scope", "mcp.tool.GetRandomNumber"),
            new Claim("scope", "mcp.tool.Echo"),
        ], "test"));

        var httpContext = CreateHttpContext(principal);
        var strategy = CreateStrategy(httpContext);
        var names = new[] { "GetRandomNumber", "Echo", "GetTimestamp" };

        var result = strategy.FilterPrimitives(McpPrimitiveType.Tool, names).ToList();

        await Assert.That(result).Count().IsEqualTo(2);
        await Assert.That(result).Contains("GetRandomNumber");
        await Assert.That(result).Contains("Echo");
        await Assert.That(result).DoesNotContain("GetTimestamp");
    }

    [Test]
    public async Task AuthenticatedUser_WithToolsAllScope_ReturnsAllTools()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("scope", "mcp.tools.all"),
        ], "test"));

        var httpContext = CreateHttpContext(principal);
        var strategy = CreateStrategy(httpContext);
        var names = new[] { "GetRandomNumber", "Echo", "GetTimestamp" };

        var result = strategy.FilterPrimitives(McpPrimitiveType.Tool, names).ToList();

        await Assert.That(result).Count().IsEqualTo(3);
    }

    [Test]
    public async Task AuthenticatedUser_WithNoToolScopes_ReturnsEmpty()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", "user123"),
        ], "test"));

        var httpContext = CreateHttpContext(principal);
        var strategy = CreateStrategy(httpContext);
        var names = new[] { "GetRandomNumber", "Echo", "GetTimestamp" };

        var result = strategy.FilterPrimitives(McpPrimitiveType.Tool, names).ToList();

        await Assert.That(result).Count().IsEqualTo(0);
    }

    [Test]
    public async Task UnauthenticatedUser_ReturnsAllToolsUnchanged()
    {
        var strategy = CreateStrategy(CreateHttpContext());
        var names = new[] { "GetRandomNumber", "Echo", "GetTimestamp" };

        var result = strategy.FilterPrimitives(McpPrimitiveType.Tool, names).ToList();

        await Assert.That(result).Count().IsEqualTo(3);
    }

    [Test]
    public async Task NullHttpContext_ReturnsAllToolsUnchanged()
    {
        var strategy = CreateStrategy(null);
        var names = new[] { "GetRandomNumber", "Echo" };

        var result = strategy.FilterPrimitives(McpPrimitiveType.Tool, names).ToList();

        await Assert.That(result).Count().IsEqualTo(2);
    }

    [Test]
    public async Task AuthenticatedUser_WithMatchingResourceScopes_ReturnsOnlyClaimed()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("scope", "mcp.resource.Server Info"),
            new Claim("scope", "mcp.resource.Process Info"),
        ], "test"));

        var httpContext = CreateHttpContext(principal);
        var strategy = CreateStrategy(httpContext);
        var names = new[] { "Server Info", "Process Info", "Current Time" };

        var result = strategy.FilterPrimitives(McpPrimitiveType.Resource, names).ToList();

        await Assert.That(result).Count().IsEqualTo(2);
        await Assert.That(result).Contains("Server Info");
        await Assert.That(result).Contains("Process Info");
    }

    [Test]
    public async Task AuthenticatedUser_WithResourcesAllScope_ReturnsAllResources()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("scope", "mcp.resources.all"),
        ], "test"));

        var httpContext = CreateHttpContext(principal);
        var strategy = CreateStrategy(httpContext);
        var names = new[] { "Server Info", "City Weather", "Process Info", "Current Time" };

        var result = strategy.FilterPrimitives(McpPrimitiveType.Resource, names).ToList();

        await Assert.That(result).Count().IsEqualTo(4);
    }

    [Test]
    public async Task AuthenticatedUser_WithNoResourceClaims_ReturnsEmpty()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("email", "user@example.com"),
        ], "test"));

        var httpContext = CreateHttpContext(principal);
        var strategy = CreateStrategy(httpContext);
        var names = new[] { "Server Info", "Process Info" };

        var result = strategy.FilterPrimitives(McpPrimitiveType.Resource, names).ToList();

        await Assert.That(result).Count().IsEqualTo(0);
    }

    [Test]
    public async Task AuthenticatedUser_WithMatchingPromptScopes_ReturnsOnlyClaimed()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("scope", "mcp.prompt.Greeting"),
        ], "test"));

        var httpContext = CreateHttpContext(principal);
        var strategy = CreateStrategy(httpContext);
        var names = new[] { "Greeting", "Help", "SecretPrompt" };

        var result = strategy.FilterPrimitives(McpPrimitiveType.Prompt, names).ToList();

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result).Contains("Greeting");
    }

    [Test]
    public async Task AuthenticatedUser_WithPromptsAllScope_ReturnsAllPrompts()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("scope", "mcp.prompts.all"),
        ], "test"));

        var httpContext = CreateHttpContext(principal);
        var strategy = CreateStrategy(httpContext);
        var names = new[] { "Greeting", "Help", "SecretPrompt" };

        var result = strategy.FilterPrimitives(McpPrimitiveType.Prompt, names).ToList();

        await Assert.That(result).Count().IsEqualTo(3);
    }

    [Test]
    public async Task AuthenticatedUser_WithNoPromptClaims_ReturnsEmpty()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("email", "user@example.com"),
        ], "test"));

        var httpContext = CreateHttpContext(principal);
        var strategy = CreateStrategy(httpContext);
        var names = new[] { "Greeting", "Help" };

        var result = strategy.FilterPrimitives(McpPrimitiveType.Prompt, names).ToList();

        await Assert.That(result).Count().IsEqualTo(0);
    }

    [Test]
    public async Task ToolScope_DoesNotGrantResourceAccess()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("scope", "mcp.tool.GetRandomNumber"),
            new Claim("scope", "mcp.tools.all"),
        ], "test"));

        var httpContext = CreateHttpContext(principal);
        var strategy = CreateStrategy(httpContext);

        var toolResult = strategy.FilterPrimitives(McpPrimitiveType.Tool,
            new[] { "GetRandomNumber", "Echo" }).ToList();
        var resourceResult = strategy.FilterPrimitives(McpPrimitiveType.Resource,
            new[] { "Server Info" }).ToList();

        await Assert.That(toolResult).Count().IsEqualTo(2);
        await Assert.That(resourceResult).Count().IsEqualTo(0);
    }
}
