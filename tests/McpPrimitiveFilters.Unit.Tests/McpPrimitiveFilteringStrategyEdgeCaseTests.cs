using Microsoft.AspNetCore.Http;

namespace McpPrimitiveFilters.Unit.Tests;

public class McpPrimitiveFilteringStrategyEdgeCaseTests
{
    private static DefaultHttpContext EmptyContext => new();

    [Test]
    public async Task FilterPrimitives_DispatchesToCorrectOverride_ForEachType()
    {
        var strategy = new CountingStrategy();

        strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Tool, new[] { "a" }).ToList();
        strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Resource, new[] { "b" }).ToList();
        strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Prompt, new[] { "c" }).ToList();

        await Assert.That(strategy.ToolCallCount).IsEqualTo(1);
        await Assert.That(strategy.ResourceCallCount).IsEqualTo(1);
        await Assert.That(strategy.PromptCallCount).IsEqualTo(1);
    }

    [Test]
    public async Task CustomStrategy_CanOverrideAllMethods()
    {
        var strategy = new ReverseStrategy();

        var result = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Tool,
            new[] { "a", "b", "c" }).ToList();

        await Assert.That(result).IsEquivalentTo(["c", "b", "a"]);
    }

    [Test]
    public async Task CustomStrategy_CanOverrideSingleMethod()
    {
        var strategy = new ToolOnlyStrategy();

        var toolResult = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Tool,
            new[] { "a", "b" }).ToList();
        var resourceResult = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Resource,
            new[] { "a", "b" }).ToList();

        await Assert.That(toolResult).Count().IsEqualTo(1);
        await Assert.That(toolResult).Contains("a");
        await Assert.That(resourceResult).Count().IsEqualTo(2);
    }

    private sealed class CountingStrategy : McpPrimitiveFilteringStrategy
    {
        public int ToolCallCount { get; private set; }
        public int ResourceCallCount { get; private set; }
        public int PromptCallCount { get; private set; }

        protected override IEnumerable<string> FilterTools(HttpContext ctx, IEnumerable<string> names)
        {
            ToolCallCount++;
            return names;
        }

        protected override IEnumerable<string> FilterResources(HttpContext ctx, IEnumerable<string> names)
        {
            ResourceCallCount++;
            return names;
        }

        protected override IEnumerable<string> FilterPrompts(HttpContext ctx, IEnumerable<string> names)
        {
            PromptCallCount++;
            return names;
        }
    }

    private sealed class ReverseStrategy : McpPrimitiveFilteringStrategy
    {
        protected override IEnumerable<string> FilterTools(HttpContext ctx, IEnumerable<string> names)
            => names.Reverse();
    }

    private sealed class ToolOnlyStrategy : McpPrimitiveFilteringStrategy
    {
        protected override IEnumerable<string> FilterTools(HttpContext ctx, IEnumerable<string> names)
            => names.Take(1);
    }
}
