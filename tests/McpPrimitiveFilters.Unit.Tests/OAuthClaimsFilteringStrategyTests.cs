using System.Security.Claims;
using McpPrimitiveFilters;
using McpPrimitiveFilters.Strategies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Unit.Tests.McpPrimitiveFilters;

public class OAuthClaimsFilteringStrategyTests
{
    private readonly OAuthClaimsFilteringStrategy _strategy = new(
        NullLogger<OAuthClaimsFilteringStrategy>.Instance);

    private static DefaultHttpContext CreateHttpContext(ClaimsPrincipal? user = null)
        => new() { User = user ?? new ClaimsPrincipal() };

    // ── Tools ──

    [Test]
    public async Task AuthenticatedUser_WithMatchingToolScopes_ReturnsOnlyClaimedTools()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("scope", "mcp.tool.GetRandomNumber"),
            new Claim("scope", "mcp.tool.Echo"),
        ], "test"));

        var ctx = CreateHttpContext(principal);
        var names = new[] { "GetRandomNumber", "Echo", "GetTimestamp" };

        var result = _strategy.FilterPrimitives(ctx, McpPrimitiveType.Tool, names).ToList();

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

        var ctx = CreateHttpContext(principal);
        var names = new[] { "GetRandomNumber", "Echo", "GetTimestamp" };

        var result = _strategy.FilterPrimitives(ctx, McpPrimitiveType.Tool, names).ToList();

        await Assert.That(result).Count().IsEqualTo(3);
    }

    [Test]
    public async Task AuthenticatedUser_WithNoToolScopes_ReturnsEmpty()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", "user123"),
        ], "test"));

        var ctx = CreateHttpContext(principal);
        var names = new[] { "GetRandomNumber", "Echo", "GetTimestamp" };

        var result = _strategy.FilterPrimitives(ctx, McpPrimitiveType.Tool, names).ToList();

        await Assert.That(result).Count().IsEqualTo(0);
    }

    [Test]
    public async Task UnauthenticatedUser_ReturnsAllToolsUnchanged()
    {
        var ctx = CreateHttpContext();
        var names = new[] { "GetRandomNumber", "Echo", "GetTimestamp" };

        var result = _strategy.FilterPrimitives(ctx, McpPrimitiveType.Tool, names).ToList();

        await Assert.That(result).Count().IsEqualTo(3);
    }

    // ── Resources ──

    [Test]
    public async Task AuthenticatedUser_WithMatchingResourceScopes_ReturnsOnlyClaimed()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("scope", "mcp.resource.Server Info"),
            new Claim("scope", "mcp.resource.Process Info"),
        ], "test"));

        var ctx = CreateHttpContext(principal);
        var names = new[] { "Server Info", "Process Info", "Current Time" };

        var result = _strategy.FilterPrimitives(ctx, McpPrimitiveType.Resource, names).ToList();

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

        var ctx = CreateHttpContext(principal);
        var names = new[] { "Server Info", "City Weather", "Process Info", "Current Time" };

        var result = _strategy.FilterPrimitives(ctx, McpPrimitiveType.Resource, names).ToList();

        await Assert.That(result).Count().IsEqualTo(4);
    }

    [Test]
    public async Task AuthenticatedUser_WithNoResourceClaims_ReturnsEmpty()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("email", "user@example.com"),
        ], "test"));

        var ctx = CreateHttpContext(principal);
        var names = new[] { "Server Info", "Process Info" };

        var result = _strategy.FilterPrimitives(ctx, McpPrimitiveType.Resource, names).ToList();

        await Assert.That(result).Count().IsEqualTo(0);
    }

    [Test]
    public async Task UnauthenticatedUser_ReturnsAllResourcesUnchanged()
    {
        var ctx = CreateHttpContext();
        var names = new[] { "Server Info", "Process Info" };

        var result = _strategy.FilterPrimitives(ctx, McpPrimitiveType.Resource, names).ToList();

        await Assert.That(result).Count().IsEqualTo(2);
    }

    // ── Prompts ──

    [Test]
    public async Task AuthenticatedUser_WithMatchingPromptScopes_ReturnsOnlyClaimed()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("scope", "mcp.prompt.Greeting"),
        ], "test"));

        var ctx = CreateHttpContext(principal);
        var names = new[] { "Greeting", "Help", "SecretPrompt" };

        var result = _strategy.FilterPrimitives(ctx, McpPrimitiveType.Prompt, names).ToList();

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result).Contains("Greeting");
    }

    [Test]
    public async Task AuthenticatedUser_WithPromptsAllScope_ReturnsAllPrompts()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("scope", "mcp.prompts.all"),
        ], "test"));

        var ctx = CreateHttpContext(principal);
        var names = new[] { "Greeting", "Help", "SecretPrompt" };

        var result = _strategy.FilterPrimitives(ctx, McpPrimitiveType.Prompt, names).ToList();

        await Assert.That(result).Count().IsEqualTo(3);
    }

    [Test]
    public async Task AuthenticatedUser_WithNoPromptClaims_ReturnsEmpty()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("email", "user@example.com"),
        ], "test"));

        var ctx = CreateHttpContext(principal);
        var names = new[] { "Greeting", "Help" };

        var result = _strategy.FilterPrimitives(ctx, McpPrimitiveType.Prompt, names).ToList();

        await Assert.That(result).Count().IsEqualTo(0);
    }

    [Test]
    public async Task UnauthenticatedUser_ReturnsAllPromptsUnchanged()
    {
        var ctx = CreateHttpContext();
        var names = new[] { "Greeting", "Help" };

        var result = _strategy.FilterPrimitives(ctx, McpPrimitiveType.Prompt, names).ToList();

        await Assert.That(result).Count().IsEqualTo(2);
    }

    // ── Cross-primitive isolation ──

    [Test]
    public async Task ToolScope_DoesNotGrantResourceAccess()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("scope", "mcp.tool.GetRandomNumber"),
            new Claim("scope", "mcp.tools.all"),
        ], "test"));

        var ctx = CreateHttpContext(principal);

        var toolResult = _strategy.FilterPrimitives(ctx, McpPrimitiveType.Tool,
            new[] { "GetRandomNumber", "Echo" }).ToList();
        var resourceResult = _strategy.FilterPrimitives(ctx, McpPrimitiveType.Resource,
            new[] { "Server Info" }).ToList();

        await Assert.That(toolResult).Count().IsEqualTo(2);
        await Assert.That(resourceResult).Count().IsEqualTo(0);
    }
}
