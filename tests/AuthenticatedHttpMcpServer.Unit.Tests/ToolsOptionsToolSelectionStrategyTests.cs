using System.ComponentModel;
using System.Reflection;
using AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer.Unit.Tests;

public class ToolsOptionsToolSelectionStrategyTests
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

    private static ToolsOptionsToolSelectionStrategy CreateSut(string[]? allowedTools) =>
        new(new StubOptionsMonitor(new ToolsSelectionOptions { AllowedTools = allowedTools }));

    private static List<string> ToolNames(IEnumerable<McpServerTool> tools) =>
        tools.Select(t => t.ProtocolTool.Name).ToList();

    [Test]
    public async Task AllowedTools_IsNull_ReturnsAllTools()
    {
        var result = CreateSut(null).FilterTools(AllTools);

        await Assert.That(result.Count()).IsEqualTo(AllTools.Count);
    }

    [Test]
    public async Task AllowedTools_IsEmpty_ReturnsAllTools()
    {
        var result = CreateSut([]).FilterTools(AllTools);

        await Assert.That(result.Count()).IsEqualTo(3);
    }

    [Test]
    public async Task AllowedTools_IsNull_WithEmptyToolList_ReturnsEmpty()
    {
        var result = CreateSut(null).FilterTools([]);

        await Assert.That(result.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task AllowedTools_SingleMatchingName_ReturnsThatTool()
    {
        var result = CreateSut(["alpha"]).FilterTools(AllTools).ToList();

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].ProtocolTool.Name).IsEqualTo("alpha");
    }

    [Test]
    public async Task AllowedTools_MultipleMatchingNames_ReturnsAllMatching()
    {
        var result = CreateSut(["alpha", "gamma"]).FilterTools(AllTools);

        await Assert.That(ToolNames(result)).IsEquivalentTo(["alpha", "gamma"]);
    }

    [Test]
    public async Task AllowedTools_UnknownToolName_ReturnsNoTools()
    {
        var result = CreateSut(["does_not_exist"]).FilterTools(AllTools);

        await Assert.That(result.Count()).IsEqualTo(0);
    }

    [Test]
    public async Task AllowedTools_MixOfKnownAndUnknown_ReturnsOnlyKnown()
    {
        var result = CreateSut(["beta", "does_not_exist"]).FilterTools(AllTools);

        await Assert.That(ToolNames(result)).IsEquivalentTo(["beta"]);
    }

    [Test]
    public async Task AllowedTools_UsesCurrentValueFromOptionsMonitor()
    {
        var monitor = new StubOptionsMonitor(new ToolsSelectionOptions { AllowedTools = ["alpha"] });
        var sut = new ToolsOptionsToolSelectionStrategy(monitor);

        var first = ToolNames(sut.FilterTools(AllTools));
        await Assert.That(first).IsEquivalentTo(["alpha"]);

        monitor.UpdateValue(new ToolsSelectionOptions { AllowedTools = ["beta"] });

        var second = ToolNames(sut.FilterTools(AllTools));
        await Assert.That(second).IsEquivalentTo(["beta"]);
    }

    private sealed class StubOptionsMonitor(ToolsSelectionOptions options) : IOptionsMonitor<ToolsSelectionOptions>
    {
        public ToolsSelectionOptions CurrentValue { get; private set; } = options;

        public void UpdateValue(ToolsSelectionOptions newOptions) => CurrentValue = newOptions;

        public ToolsSelectionOptions Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<ToolsSelectionOptions, string?> listener) => null;
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
