using System.Security.Claims;
using AuthenticatedHttpMcpServer.Infrastructure;
using AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer.Unit.Tests;

public class McpToolRegistryTests
{
    private static McpToolRegistry CreateRegistry()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddOptions();
        services.AddMcpServer().WithToolsFromAssembly(typeof(DemoTools).Assembly);
        return new McpToolRegistry(services.BuildServiceProvider().GetServices<McpServerTool>());
    }

    [Test]
    public async Task FilterToolsUsingStrategies_PassesAllDemoToolsToFirstStrategy()
    {
        IReadOnlyCollection<McpServerTool>? captured = null;
        var strategy = new StubStrategy(tools => { captured = [.. tools]; return tools; });

        CreateRegistry().FilterToolsUsingStrategy([strategy]).ToList();

        var names = captured!.Select(t => t.ProtocolTool.Name).ToList();
        await Assert.That(names).Contains("hello_world");
        await Assert.That(names).Contains("random_number");
    }

    [Test]
    public async Task FilterToolsUsingStrategies_ReturnsExactlyWhatSingleStrategyReturns()
    {
        var strategy = new StubStrategy(tools =>
            tools.Where(t => t.ProtocolTool.Name == "hello_world"));

        var result = CreateRegistry()
            .FilterToolsUsingStrategy([strategy])
            .Select(t => t.ProtocolTool.Name)
            .ToList();

        await Assert.That(result).IsEquivalentTo(["hello_world"]);
    }

    [Test]
    public async Task FilterToolsUsingStrategies_StrategyReturnsEmpty_YieldsNoTools()
    {
        var strategy = new StubStrategy(_ => []);

        var result = CreateRegistry()
            .FilterToolsUsingStrategy([strategy])
            .ToList();

        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task FilterToolsUsingStrategies_NoStrategies_ReturnsAllDemoTools()
    {
        var result = CreateRegistry()
            .FilterToolsUsingStrategy([])
            .Select(t => t.ProtocolTool.Name)
            .ToList();

        await Assert.That(result).Contains("hello_world");
        await Assert.That(result).Contains("random_number");
    }

    [Test]
    public async Task FilterToolsUsingStrategies_AppliesStrategiesInSequence()
    {
        // First strategy keeps only "hello_world"; second should only see that one tool.
        IReadOnlyCollection<McpServerTool>? secondInput = null;

        var first = new StubStrategy(tools =>
            tools.Where(t => t.ProtocolTool.Name == "hello_world"));
        var second = new StubStrategy(tools => { secondInput = [.. tools]; return tools; });

        CreateRegistry().FilterToolsUsingStrategy([first, second]).ToList();

        await Assert.That(secondInput!.Count).IsEqualTo(1);
        await Assert.That(secondInput!.Single().ProtocolTool.Name).IsEqualTo("hello_world");
    }

    [Test]
    public async Task FilterToolsUsingStrategies_MultipleStrategies_ReturnsIntersection()
    {
        var allowHello = new StubStrategy(tools =>
            tools.Where(t => t.ProtocolTool.Name == "hello_world"));
        var allowRandom = new StubStrategy(tools =>
            tools.Where(t => t.ProtocolTool.Name == "random_number"));

        var result = CreateRegistry()
            .FilterToolsUsingStrategy([allowHello, allowRandom])
            .ToList();

        await Assert.That(result.Count).IsEqualTo(0);
    }

    // --- Tests with real strategy implementations ---

    [Test]
    public async Task RealStrategies_BothAllow_ToolIsReturned()
    {
        // Scope: tool:random_number | Options: ["random_number"] → intersection: [random_number]
        var result = CreateRegistry()
            .FilterToolsUsingStrategy([
                ScopeStrategy("tool:random_number"),
                OptionsStrategy(["random_number"])
            ])
            .Select(t => t.ProtocolTool.Name)
            .ToList();

        await Assert.That(result).IsEquivalentTo(["random_number"]);
    }

    [Test]
    public async Task RealStrategies_ScopeAllows_OptionsBlocks_ToolNotReturned()
    {
        // Scope: tool:hello_world (allowed) | Options: ["random_number"] (blocks hello_world)
        var result = CreateRegistry()
            .FilterToolsUsingStrategy([
                ScopeStrategy("tool:hello_world"),
                OptionsStrategy(["random_number"])
            ])
            .ToList();

        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RealStrategies_ScopeBlocks_OptionsAllows_ToolNotReturned()
    {
        // Scope: tool:random_number (blocks hello_world) | Options: ["hello_world"] (allows hello_world)
        var result = CreateRegistry()
            .FilterToolsUsingStrategy([
                ScopeStrategy("tool:random_number"),
                OptionsStrategy(["hello_world"])
            ])
            .ToList();

        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RealStrategies_BothBlock_ReturnsNoTools()
    {
        // Scope: no claims (blocks all) | Options: [] (blocks all)
        var result = CreateRegistry()
            .FilterToolsUsingStrategy([
                ScopeStrategy(),
                OptionsStrategy([])
            ])
            .ToList();

        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RealStrategies_ToolAllScope_NullOptions_AllToolsReturned()
    {
        // Scope: tool:all (pass-through) | Options: null (pass-through) → all tools
        var result = CreateRegistry()
            .FilterToolsUsingStrategy([
                ScopeStrategy("tool:all"),
                OptionsStrategy(null)
            ])
            .Select(t => t.ProtocolTool.Name)
            .ToList();

        await Assert.That(result).IsEquivalentTo(["hello_world", "random_number"]);
    }

    [Test]
    public async Task RealStrategies_DisjointAllowLists_ReturnsEmptyIntersection()
    {
        // Scope allows only hello_world; options allows only random_number → no overlap
        var result = CreateRegistry()
            .FilterToolsUsingStrategy([
                ScopeStrategy("tool:hello_world"),
                OptionsStrategy(["random_number"])
            ])
            .ToList();

        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task RealStrategies_ThreeStrategies_AllMustAgree()
    {
        // A third strategy can veto tools even when the first two agree.
        var vetoAll = new StubStrategy(_ => []);

        var result = CreateRegistry()
            .FilterToolsUsingStrategy([
                ScopeStrategy("tool:all"),
                OptionsStrategy(null),
                vetoAll
            ])
            .ToList();

        await Assert.That(result.Count).IsEqualTo(0);
    }

    private static ScopeToolsClaimsPrincipalToolSelectionStrategy ScopeStrategy(
        params string[] scopes)
    {
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(
            new ClaimsIdentity(scopes.Select(s => new Claim("scope", s)), "test"));
        return new ScopeToolsClaimsPrincipalToolSelectionStrategy(
            new HttpContextAccessor { HttpContext = ctx });
    }

    private static ToolsOptionsToolSelectionStrategy OptionsStrategy(string[]? allowedTools) =>
        new(new StubOptionsMonitor(new ToolsSelectionOptions { AllowedTools = allowedTools }));

    private sealed class StubOptionsMonitor(ToolsSelectionOptions options)
        : IOptionsMonitor<ToolsSelectionOptions>
    {
        public ToolsSelectionOptions CurrentValue { get; } = options;
        public ToolsSelectionOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<ToolsSelectionOptions, string?> listener) => null;
    }

    private sealed class StubStrategy(
        Func<IEnumerable<McpServerTool>, IEnumerable<McpServerTool>> filter)
        : ToolSelectionStrategy
    {
        public IEnumerable<McpServerTool> FilterTools(IEnumerable<McpServerTool> tools) =>
            filter(tools);
    }
}
