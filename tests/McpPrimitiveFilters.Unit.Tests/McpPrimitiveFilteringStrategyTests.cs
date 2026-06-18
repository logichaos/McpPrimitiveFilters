using McpPrimitiveFilters;
using Microsoft.AspNetCore.Http;

namespace McpServer.Unit.Tests.McpPrimitiveFilters;

public class McpPrimitiveFilteringStrategyTests
{
    private static DefaultHttpContext EmptyContext => new();

    [Test]
    public async Task FilterPrimitives_DispatchesToCorrectOverride()
    {
        var strategy = new TestableStrategy();

        var toolResult = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Tool,
            new[] { "a", "b" }).ToList();
        var resourceResult = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Resource,
            new[] { "a", "b" }).ToList();
        var promptResult = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Prompt,
            new[] { "a", "b" }).ToList();

        await Assert.That(strategy.LastCalledType).IsEqualTo(McpPrimitiveType.Prompt);
        await Assert.That(toolResult).Count().IsEqualTo(0);
        await Assert.That(resourceResult).Count().IsEqualTo(2);
        await Assert.That(promptResult).Count().IsEqualTo(2);
    }

    [Test]
    public async Task DefaultImplementation_ReturnsNamesUnchanged()
    {
        var strategy = new DefaultPassThroughStrategy();
        var names = new[] { "one", "two", "three" };

        var toolResult = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Tool, names).ToList();
        var resourceResult = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Resource, names).ToList();
        var promptResult = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Prompt, names).ToList();

        await Assert.That(toolResult).IsEquivalentTo(names);
        await Assert.That(resourceResult).IsEquivalentTo(names);
        await Assert.That(promptResult).IsEquivalentTo(names);
    }

    private sealed class TestableStrategy : McpPrimitiveFilteringStrategy
    {
        public McpPrimitiveType LastCalledType { get; private set; }

        protected override IEnumerable<string> FilterTools(HttpContext ctx, IEnumerable<string> names)
        {
            LastCalledType = McpPrimitiveType.Tool;
            return [];
        }

        protected override IEnumerable<string> FilterResources(HttpContext ctx, IEnumerable<string> names)
        {
            LastCalledType = McpPrimitiveType.Resource;
            return names;
        }

        protected override IEnumerable<string> FilterPrompts(HttpContext ctx, IEnumerable<string> names)
        {
            LastCalledType = McpPrimitiveType.Prompt;
            return names;
        }
    }

    private sealed class DefaultPassThroughStrategy : McpPrimitiveFilteringStrategy
    {
    }
}
