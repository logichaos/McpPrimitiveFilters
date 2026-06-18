using McpPrimitiveFilters.Strategies;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace McpPrimitiveFilters.Unit.Tests;

public class AppSettingsPrimitiveFilteringStrategyExtendedTests
{
    private static DefaultHttpContext EmptyContext => new();

    [Test]
    public async Task AllThreeSections_Configured_EachFiltersIndependently()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["McpFiltering:Allowed:tools:0"] = "ToolA",
                ["McpFiltering:Allowed:resources:0"] = "ResourceA",
                ["McpFiltering:Allowed:prompts:0"] = "PromptA",
            })
            .Build();

        var strategy = new AppSettingsPrimitiveFilteringStrategy(config);

        var toolsResult = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Tool,
            new[] { "ToolA", "ToolB" }).ToList();
        var resourcesResult = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Resource,
            new[] { "ResourceA", "ResourceB" }).ToList();
        var promptsResult = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Prompt,
            new[] { "PromptA", "PromptB" }).ToList();

        await Assert.That(toolsResult).Count().IsEqualTo(1);
        await Assert.That(resourcesResult).Count().IsEqualTo(1);
        await Assert.That(promptsResult).Count().IsEqualTo(1);
    }

    [Test]
    public async Task EmptyNameList_ReturnsEmptyList()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["McpFiltering:Allowed:tools:0"] = "ToolA",
            })
            .Build();

        var strategy = new AppSettingsPrimitiveFilteringStrategy(config);

        var result = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Tool,
            Array.Empty<string>()).ToList();

        await Assert.That(result).Count().IsEqualTo(0);
    }

    [Test]
    public async Task SingleName_Allowed_ReturnsIt()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["McpFiltering:Allowed:tools:0"] = "OnlyTool",
            })
            .Build();

        var strategy = new AppSettingsPrimitiveFilteringStrategy(config);

        var result = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Tool,
            new[] { "OnlyTool" }).ToList();

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result[0]).IsEqualTo("OnlyTool");
    }

    [Test]
    public async Task SingleName_Blocked_ReturnsEmpty()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["McpFiltering:Allowed:tools:0"] = "OtherTool",
            })
            .Build();

        var strategy = new AppSettingsPrimitiveFilteringStrategy(config);

        var result = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Tool,
            new[] { "MyTool" }).ToList();

        await Assert.That(result).Count().IsEqualTo(0);
    }

    [Test]
    public async Task EmptyConfigSection_ReturnsAll()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var strategy = new AppSettingsPrimitiveFilteringStrategy(config);

        var result = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Tool,
            new[] { "ToolA", "ToolB", "ToolC" }).ToList();

        await Assert.That(result).Count().IsEqualTo(3);
    }
}
