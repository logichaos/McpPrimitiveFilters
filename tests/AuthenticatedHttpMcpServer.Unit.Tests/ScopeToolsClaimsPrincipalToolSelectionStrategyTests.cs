using System.ComponentModel;
using System.Reflection;
using System.Security.Claims;
using AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer.Unit.Tests;

public class ScopeToolsClaimsPrincipalToolSelectionStrategyTests
{
    private static readonly IReadOnlyCollection<McpServerTool> AllTools = BuildStubTools();

    private static IReadOnlyCollection<McpServerTool> BuildStubTools()
    {
        var target = new StubTools();
        return typeof(StubTools)
            .GetMethods()
            .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() is not null)
            .Select(m => McpServerTool.Create(m, target, new McpServerToolCreateOptions()))
            .ToArray();
    }

    private static HttpContext ContextWithScopes(params string[] scopes)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity(scopes.Select(s => new Claim("scope", s)), "test"));
        return context;
    }

    private static List<string> ToolNames(IEnumerable<McpServerTool> tools) =>
        tools.Select(t => t.ProtocolTool.Name).ToList();

    private readonly ScopeToolsClaimsPrincipalToolSelectionStrategy _sut = new();

    [Test]
    public async Task NoScopeClaims_ReturnsNoTools()
    {
        var result = _sut.FilterToolsWithStrategy(AllTools, ContextWithScopes());

        await Assert.That(result.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task UnauthenticatedUser_ReturnsNoTools()
    {
        var result = _sut.FilterToolsWithStrategy(AllTools, new DefaultHttpContext());

        await Assert.That(result.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task ToolAllScope_ReturnsEveryTool()
    {
        var result = _sut.FilterToolsWithStrategy(AllTools, ContextWithScopes("tool:all")).ToList();

        await Assert.That(result.Count).IsEqualTo(AllTools.Count);
    }

    [Test]
    public async Task ToolAllScope_WithEmptyToolList_ReturnsEmpty()
    {
        var result = _sut.FilterToolsWithStrategy([], ContextWithScopes("tool:all"));

        await Assert.That(result.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task SingleToolScope_ReturnsOnlyThatTool()
    {
        var result = _sut.FilterToolsWithStrategy(AllTools, ContextWithScopes("tool:alpha")).ToList();

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].ProtocolTool.Name).IsEqualTo("alpha");
    }

    [Test]
    public async Task MultipleToolScopes_ReturnsAllMatchedTools()
    {
        var result = _sut.FilterToolsWithStrategy(
            AllTools, ContextWithScopes("tool:alpha", "tool:gamma"));

        await Assert.That(ToolNames(result)).IsEquivalentTo(["alpha", "gamma"]);
    }

    [Test]
    public async Task ScopeWithoutToolPrefix_DoesNotMatchAnyTool()
    {
        // "alpha" ≠ "tool:alpha"
        var result = _sut.FilterToolsWithStrategy(AllTools, ContextWithScopes("alpha"));

        await Assert.That(result.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task ToolScopeForUnknownTool_ReturnsNoTools()
    {
        var result = _sut.FilterToolsWithStrategy(AllTools, ContextWithScopes("tool:does_not_exist"));

        await Assert.That(result.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task MixedScopes_OnlyToolPrefixedOnesMatch()
    {
        // "openid" and "profile" are common OAuth scopes that must not leak tools
        var result = _sut.FilterToolsWithStrategy(
            AllTools, ContextWithScopes("openid", "profile", "tool:beta"));

        await Assert.That(ToolNames(result)).IsEquivalentTo(["beta"]);
    }

    private sealed class StubTools
    {
        [McpServerTool(Name = "alpha"), Description("Alpha")]
        public string Alpha() => "";

        [McpServerTool(Name = "beta"), Description("Beta")]
        public string Beta() => "";

        [McpServerTool(Name = "gamma"), Description("Gamma")]
        public string Gamma() => "";
    }
}
