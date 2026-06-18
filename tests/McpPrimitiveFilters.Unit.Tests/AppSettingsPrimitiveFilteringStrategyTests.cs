using McpPrimitiveFilters;
using McpPrimitiveFilters.Strategies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace McpServer.Unit.Tests.McpPrimitiveFilters;

public class AppSettingsPrimitiveFilteringStrategyTests
{
    private static DefaultHttpContext EmptyContext => new();

    private static IConfiguration CreateConfiguration(string? json)
    {
        var builder = new ConfigurationBuilder();
        if (json is not null)
            builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)));
        return builder.Build();
    }

    // ── Tools ──

    [Test]
    public async Task AllowedTools_ContainsSubset_ReturnsOnlyMatchingTools()
    {
        var config = CreateConfiguration("""
        {
            "McpFiltering": {
                "Allowed": {
                    "tools": ["GetRandomNumber", "Echo"]
                }
            }
        }
        """);

        var strategy = new AppSettingsPrimitiveFilteringStrategy(config);
        var names = new[] { "GetRandomNumber", "Echo", "GetTimestamp", "ListUsers", "GetServerStats" };

        var result = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Tool, names).ToList();

        await Assert.That(result).Count().IsEqualTo(2);
        await Assert.That(result).Contains("GetRandomNumber");
        await Assert.That(result).Contains("Echo");
    }

    [Test]
    public async Task AllowedTools_IsEmptyArray_ReturnsAllToolsUnchanged()
    {
        var config = CreateConfiguration("""
        { "McpFiltering": { "Allowed": { "tools": [] } } }
        """);

        var strategy = new AppSettingsPrimitiveFilteringStrategy(config);
        var names = new[] { "GetRandomNumber", "Echo" };

        var result = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Tool, names).ToList();

        await Assert.That(result).Count().IsEqualTo(2);
    }

    [Test]
    public async Task AllowedTools_KeyMissing_ReturnsAllToolsUnchanged()
    {
        var config = CreateConfiguration("""
        { "McpFiltering": { "Allowed": {} } }
        """);

        var strategy = new AppSettingsPrimitiveFilteringStrategy(config);
        var names = new[] { "GetRandomNumber", "Echo" };

        var result = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Tool, names).ToList();

        await Assert.That(result).Count().IsEqualTo(2);
    }

    [Test]
    public async Task AllowedTools_NameMatchingIsCaseInsensitive()
    {
        var config = CreateConfiguration("""
        { "McpFiltering": { "Allowed": { "tools": ["getrandomnumber"] } } }
        """);

        var strategy = new AppSettingsPrimitiveFilteringStrategy(config);
        var names = new[] { "GetRandomNumber", "Echo" };

        var result = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Tool, names).ToList();

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result).Contains("GetRandomNumber");
    }

    [Test]
    public async Task AllowedTools_ReferencesNonExistentTool_ReturnsEmptyList()
    {
        var config = CreateConfiguration("""
        { "McpFiltering": { "Allowed": { "tools": ["NonExistentTool"] } } }
        """);

        var strategy = new AppSettingsPrimitiveFilteringStrategy(config);
        var names = new[] { "GetRandomNumber", "Echo" };

        var result = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Tool, names).ToList();

        await Assert.That(result).Count().IsEqualTo(0);
    }

    // ── Resources ──

    [Test]
    public async Task AllowedResources_ContainsSubset_ReturnsOnlyMatchingResources()
    {
        var config = CreateConfiguration("""
        { "McpFiltering": { "Allowed": { "resources": ["Server Info", "Current Time"] } } }
        """);

        var strategy = new AppSettingsPrimitiveFilteringStrategy(config);
        var names = new[] { "Server Info", "City Weather", "Process Info", "Current Time" };

        var result = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Resource, names).ToList();

        await Assert.That(result).Count().IsEqualTo(2);
        await Assert.That(result).Contains("Server Info");
        await Assert.That(result).Contains("Current Time");
    }

    [Test]
    public async Task AllowedResources_IsEmptyArray_ReturnsAllUnchanged()
    {
        var config = CreateConfiguration("""
        { "McpFiltering": { "Allowed": { "resources": [] } } }
        """);

        var strategy = new AppSettingsPrimitiveFilteringStrategy(config);
        var names = new[] { "Server Info", "Process Info" };

        var result = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Resource, names).ToList();

        await Assert.That(result).Count().IsEqualTo(2);
    }

    [Test]
    public async Task AllowedResources_KeyMissing_ReturnsAllUnchanged()
    {
        var config = CreateConfiguration("""
        { "McpFiltering": { "Allowed": {} } }
        """);

        var strategy = new AppSettingsPrimitiveFilteringStrategy(config);
        var names = new[] { "Server Info", "Process Info" };

        var result = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Resource, names).ToList();

        await Assert.That(result).Count().IsEqualTo(2);
    }

    [Test]
    public async Task AllowedResources_NameMatchingIsCaseInsensitive()
    {
        var config = CreateConfiguration("""
        { "McpFiltering": { "Allowed": { "resources": ["server info"] } } }
        """);

        var strategy = new AppSettingsPrimitiveFilteringStrategy(config);
        var names = new[] { "Server Info", "Process Info" };

        var result = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Resource, names).ToList();

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result).Contains("Server Info");
    }

    // ── Prompts ──

    [Test]
    public async Task AllowedPrompts_ContainsSubset_ReturnsOnlyMatchingPrompts()
    {
        var config = CreateConfiguration("""
        { "McpFiltering": { "Allowed": { "prompts": ["Greeting", "Help"] } } }
        """);

        var strategy = new AppSettingsPrimitiveFilteringStrategy(config);
        var names = new[] { "Greeting", "Help", "SecretPrompt" };

        var result = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Prompt, names).ToList();

        await Assert.That(result).Count().IsEqualTo(2);
        await Assert.That(result).Contains("Greeting");
        await Assert.That(result).Contains("Help");
    }

    [Test]
    public async Task AllowedPrompts_KeyMissing_ReturnsAllUnchanged()
    {
        var config = CreateConfiguration("""
        { "McpFiltering": { "Allowed": {} } }
        """);

        var strategy = new AppSettingsPrimitiveFilteringStrategy(config);
        var names = new[] { "Greeting", "Help" };

        var result = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Prompt, names).ToList();

        await Assert.That(result).Count().IsEqualTo(2);
    }

    // ── Cross-primitive isolation ──

    [Test]
    public async Task AllowedTools_DoesNotAffectResources()
    {
        var config = CreateConfiguration("""
        { "McpFiltering": { "Allowed": { "tools": ["MyTool"], "resources": ["MyResource"] } } }
        """);

        var strategy = new AppSettingsPrimitiveFilteringStrategy(config);

        var toolResult = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Tool,
            new[] { "MyTool", "OtherTool" }).ToList();
        var resourceResult = strategy.FilterPrimitives(EmptyContext, McpPrimitiveType.Resource,
            new[] { "MyResource", "OtherResource" }).ToList();

        await Assert.That(toolResult).Count().IsEqualTo(1);
        await Assert.That(toolResult).Contains("MyTool");
        await Assert.That(resourceResult).Count().IsEqualTo(1);
        await Assert.That(resourceResult).Contains("MyResource");
    }
}
