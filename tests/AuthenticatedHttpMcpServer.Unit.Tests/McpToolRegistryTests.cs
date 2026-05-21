using System.Security.Claims;
using AuthenticatedHttpMcpServer.Infrastructure;
using AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer.Unit.Tests;

public class McpToolRegistryTests
{
    private static McpToolRegistry CreateRegistry()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        return new McpToolRegistry(services.BuildServiceProvider());
    }

    [Test]
    public async Task GetToolsForClaimsPrincipal_PassesAllDemoToolsToStrategy()
    {
        IReadOnlyCollection<McpServerTool>? captured = null;
        var strategy = new StubStrategy((tools, _) => { captured = tools; return tools; });

        CreateRegistry().GetToolsForClaimsPrincipal(new DefaultHttpContext(), strategy).ToList();

        var names = captured!.Select(t => t.ProtocolTool.Name).ToList();
        await Assert.That(names).Contains("hello_world");
        await Assert.That(names).Contains("random_number");
    }

    [Test]
    public async Task GetToolsForClaimsPrincipal_PassesHttpContextToStrategy()
    {
        HttpContext? captured = null;
        var strategy = new StubStrategy((tools, ctx) => { captured = ctx; return tools; });
        var context = new DefaultHttpContext();

        CreateRegistry().GetToolsForClaimsPrincipal(context, strategy).ToList();

        await Assert.That(ReferenceEquals(captured, context)).IsTrue();
    }

    [Test]
    public async Task GetToolsForClaimsPrincipal_ReturnsExactlyWhatStrategyReturns()
    {
        var strategy = new StubStrategy((tools, _) =>
            tools.Where(t => t.ProtocolTool.Name == "hello_world"));

        var result = CreateRegistry()
            .GetToolsForClaimsPrincipal(new DefaultHttpContext(), strategy)
            .Select(t => t.ProtocolTool.Name)
            .ToList();

        await Assert.That(result).IsEquivalentTo(["hello_world"]);
    }

    [Test]
    public async Task GetToolsForClaimsPrincipal_StrategyReturnsEmpty_YieldsNoTools()
    {
        var strategy = new StubStrategy((_, _) => []);

        var result = CreateRegistry()
            .GetToolsForClaimsPrincipal(new DefaultHttpContext(), strategy)
            .ToList();

        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetToolsForClaimsPrincipal_WithToolAllScope_ReturnsBothDemoTools()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("scope", "tool:all")], "test"));

        var result = CreateRegistry()
            .GetToolsForClaimsPrincipal(context, new ScopeToolsClaimsPrincipalToolSelectionStrategy())
            .Select(t => t.ProtocolTool.Name)
            .ToList();

        await Assert.That(result).IsEquivalentTo(["hello_world", "random_number"]);
    }

    [Test]
    public async Task GetToolsForClaimsPrincipal_WithSpecificToolScope_ReturnsOnlyThatTool()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("scope", "tool:hello_world")], "test"));

        var result = CreateRegistry()
            .GetToolsForClaimsPrincipal(context, new ScopeToolsClaimsPrincipalToolSelectionStrategy())
            .Select(t => t.ProtocolTool.Name)
            .ToList();

        await Assert.That(result).IsEquivalentTo(["hello_world"]);
    }

    [Test]
    public async Task GetToolsForClaimsPrincipal_WithNoScope_ReturnsNoTools()
    {
        var result = CreateRegistry()
            .GetToolsForClaimsPrincipal(
                new DefaultHttpContext(),
                new ScopeToolsClaimsPrincipalToolSelectionStrategy())
            .ToList();

        await Assert.That(result.Count).IsEqualTo(0);
    }

    private sealed class StubStrategy(
        Func<IReadOnlyCollection<McpServerTool>, HttpContext, IEnumerable<McpServerTool>> filter)
        : HttpContextToolSelectionStrategy
    {
        public IEnumerable<McpServerTool> FilterToolsWithStrategy(
            IReadOnlyCollection<McpServerTool> tools, HttpContext ctx) => filter(tools, ctx);
    }
}
