using McpPrimitiveFilters;
using McpPrimitiveFilters.Strategies;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace McpPrimitiveFilters.Unit.Tests;

public class AppSettingsPrimitiveFilteringStrategyTests
{
  private static IOptions<McpFilteringOptions> CreateOptions(string? json)
  {
    var builder = new ConfigurationBuilder();
    if (json is not null)
      builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)));
    var config = builder.Build();
    return Options.Create(config.GetSection("McpFiltering").Get<McpFilteringOptions>() ?? new McpFilteringOptions());
  }

  [Test]
  public async Task AllowedTools_ContainsSubset_ReturnsOnlyMatchingTools()
  {
    var options = CreateOptions("""
        {
            "McpFiltering": {
                "Allowed": {
                    "tools": ["GetRandomNumber", "Echo"]
                }
            }
        }
        """);

    var strategy = new AppSettingsPrimitiveFilteringStrategy(options, NullLogger<AppSettingsPrimitiveFilteringStrategy>.Instance);
    var names = new[] { "GetRandomNumber", "Echo", "GetTimestamp", "ListUsers", "GetServerStats" };

    var result = strategy.FilterPrimitives(McpPrimitiveType.Tool, names).ToList();

    await Assert.That(result).Count().IsEqualTo(2);
    await Assert.That(result).Contains("GetRandomNumber");
    await Assert.That(result).Contains("Echo");
  }

  [Test]
  public async Task AllowedTools_IsEmptyArray_ReturnsAllToolsUnchanged()
  {
    var options = CreateOptions("""
        { "McpFiltering": { "Allowed": { "tools": [] } } }
        """);

    var strategy = new AppSettingsPrimitiveFilteringStrategy(options, NullLogger<AppSettingsPrimitiveFilteringStrategy>.Instance);
    var names = new[] { "GetRandomNumber", "Echo" };

    var result = strategy.FilterPrimitives(McpPrimitiveType.Tool, names).ToList();

    await Assert.That(result).Count().IsEqualTo(2);
  }

  [Test]
  public async Task AllowedTools_KeyMissing_ReturnsAllToolsUnchanged()
  {
    var options = CreateOptions("""
        { "McpFiltering": { "Allowed": {} } }
        """);

    var strategy = new AppSettingsPrimitiveFilteringStrategy(options, NullLogger<AppSettingsPrimitiveFilteringStrategy>.Instance);
    var names = new[] { "GetRandomNumber", "Echo" };

    var result = strategy.FilterPrimitives(McpPrimitiveType.Tool, names).ToList();

    await Assert.That(result).Count().IsEqualTo(2);
  }

  [Test]
  public async Task AllowedTools_NameMatchingIsCaseInsensitive()
  {
    var options = CreateOptions("""
        { "McpFiltering": { "Allowed": { "tools": ["getrandomnumber"] } } }
        """);

    var strategy = new AppSettingsPrimitiveFilteringStrategy(options, NullLogger<AppSettingsPrimitiveFilteringStrategy>.Instance);
    var names = new[] { "GetRandomNumber", "Echo" };

    var result = strategy.FilterPrimitives(McpPrimitiveType.Tool, names).ToList();

    await Assert.That(result).Count().IsEqualTo(1);
    await Assert.That(result).Contains("GetRandomNumber");
  }

  [Test]
  public async Task AllowedTools_ReferencesNonExistentTool_ReturnsEmptyList()
  {
    var options = CreateOptions("""
        { "McpFiltering": { "Allowed": { "tools": ["NonExistentTool"] } } }
        """);

    var strategy = new AppSettingsPrimitiveFilteringStrategy(options, NullLogger<AppSettingsPrimitiveFilteringStrategy>.Instance);
    var names = new[] { "GetRandomNumber", "Echo" };

    var result = strategy.FilterPrimitives(McpPrimitiveType.Tool, names).ToList();

    await Assert.That(result).Count().IsEqualTo(0);
  }

  [Test]
  public async Task AllowedResources_ContainsSubset_ReturnsOnlyMatchingResources()
  {
    var options = CreateOptions("""
        { "McpFiltering": { "Allowed": { "resources": ["Server Info", "Current Time"] } } }
        """);

    var strategy = new AppSettingsPrimitiveFilteringStrategy(options, NullLogger<AppSettingsPrimitiveFilteringStrategy>.Instance);
    var names = new[] { "Server Info", "City Weather", "Process Info", "Current Time" };

    var result = strategy.FilterPrimitives(McpPrimitiveType.Resource, names).ToList();

    await Assert.That(result).Count().IsEqualTo(2);
    await Assert.That(result).Contains("Server Info");
    await Assert.That(result).Contains("Current Time");
  }

  [Test]
  public async Task AllowedResources_IsEmptyArray_ReturnsAllUnchanged()
  {
    var options = CreateOptions("""
        { "McpFiltering": { "Allowed": { "resources": [] } } }
        """);

    var strategy = new AppSettingsPrimitiveFilteringStrategy(options, NullLogger<AppSettingsPrimitiveFilteringStrategy>.Instance);
    var names = new[] { "Server Info", "Process Info" };

    var result = strategy.FilterPrimitives(McpPrimitiveType.Resource, names).ToList();

    await Assert.That(result).Count().IsEqualTo(2);
  }

  [Test]
  public async Task AllowedResources_KeyMissing_ReturnsAllUnchanged()
  {
    var options = CreateOptions("""
        { "McpFiltering": { "Allowed": {} } }
        """);

    var strategy = new AppSettingsPrimitiveFilteringStrategy(options, NullLogger<AppSettingsPrimitiveFilteringStrategy>.Instance);
    var names = new[] { "Server Info", "Process Info" };

    var result = strategy.FilterPrimitives(McpPrimitiveType.Resource, names).ToList();

    await Assert.That(result).Count().IsEqualTo(2);
  }

  [Test]
  public async Task AllowedResources_NameMatchingIsCaseInsensitive()
  {
    var options = CreateOptions("""
        { "McpFiltering": { "Allowed": { "resources": ["server info"] } } }
        """);

    var strategy = new AppSettingsPrimitiveFilteringStrategy(options, NullLogger<AppSettingsPrimitiveFilteringStrategy>.Instance);
    var names = new[] { "Server Info", "Process Info" };

    var result = strategy.FilterPrimitives(McpPrimitiveType.Resource, names).ToList();

    await Assert.That(result).Count().IsEqualTo(1);
    await Assert.That(result).Contains("Server Info");
  }

  [Test]
  public async Task AllowedPrompts_ContainsSubset_ReturnsOnlyMatchingPrompts()
  {
    var options = CreateOptions("""
        { "McpFiltering": { "Allowed": { "prompts": ["Greeting", "Help"] } } }
        """);

    var strategy = new AppSettingsPrimitiveFilteringStrategy(options, NullLogger<AppSettingsPrimitiveFilteringStrategy>.Instance);
    var names = new[] { "Greeting", "Help", "SecretPrompt" };

    var result = strategy.FilterPrimitives(McpPrimitiveType.Prompt, names).ToList();

    await Assert.That(result).Count().IsEqualTo(2);
    await Assert.That(result).Contains("Greeting");
    await Assert.That(result).Contains("Help");
  }

  [Test]
  public async Task AllowedPrompts_KeyMissing_ReturnsAllUnchanged()
  {
    var options = CreateOptions("""
        { "McpFiltering": { "Allowed": {} } }
        """);

    var strategy = new AppSettingsPrimitiveFilteringStrategy(options, NullLogger<AppSettingsPrimitiveFilteringStrategy>.Instance);
    var names = new[] { "Greeting", "Help" };

    var result = strategy.FilterPrimitives(McpPrimitiveType.Prompt, names).ToList();

    await Assert.That(result).Count().IsEqualTo(2);
  }

  [Test]
  public async Task AllowedTools_DoesNotAffectResources()
  {
    var options = CreateOptions("""
        { "McpFiltering": { "Allowed": { "tools": ["MyTool"], "resources": ["MyResource"] } } }
        """);

    var strategy = new AppSettingsPrimitiveFilteringStrategy(options, NullLogger<AppSettingsPrimitiveFilteringStrategy>.Instance);

    var toolResult = strategy.FilterPrimitives(McpPrimitiveType.Tool,
        new[] { "MyTool", "OtherTool" }).ToList();
    var resourceResult = strategy.FilterPrimitives(McpPrimitiveType.Resource,
        new[] { "MyResource", "OtherResource" }).ToList();

    await Assert.That(toolResult).Count().IsEqualTo(1);
    await Assert.That(toolResult).Contains("MyTool");
    await Assert.That(resourceResult).Count().IsEqualTo(1);
    await Assert.That(resourceResult).Contains("MyResource");
  }
}