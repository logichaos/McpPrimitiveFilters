using McpPrimitiveFilters;
using McpPrimitiveFilters.Strategies;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace McpPrimitiveFilters.Unit.Tests;

public class AppSettingsPrimitiveFilteringStrategyExtendedTests
{
  private static IOptions<McpFilteringOptions> CreateOptions(Dictionary<string, string?>? settings = null)
  {
    var builder = new ConfigurationBuilder();
    if (settings is not null)
      builder.AddInMemoryCollection(settings);
    var config = builder.Build();
    return Options.Create(config.GetSection("McpFiltering").Get<McpFilteringOptions>() ?? new McpFilteringOptions());
  }

  [Test]
  public async Task AllThreeSections_Configured_EachFiltersIndependently()
  {
    var options = CreateOptions(new Dictionary<string, string?>
    {
      ["McpFiltering:Allowed:tools:0"] = "ToolA",
      ["McpFiltering:Allowed:resources:0"] = "ResourceA",
      ["McpFiltering:Allowed:prompts:0"] = "PromptA",
    });

    var strategy = new AppSettingsPrimitiveFilteringStrategy(options, NullLogger<AppSettingsPrimitiveFilteringStrategy>.Instance);

    var toolsResult = strategy.FilterPrimitives(McpPrimitiveType.Tool,
        new[] { "ToolA", "ToolB" }).ToList();
    var resourcesResult = strategy.FilterPrimitives(McpPrimitiveType.Resource,
        new[] { "ResourceA", "ResourceB" }).ToList();
    var promptsResult = strategy.FilterPrimitives(McpPrimitiveType.Prompt,
        new[] { "PromptA", "PromptB" }).ToList();

    await Assert.That(toolsResult).Count().IsEqualTo(1);
    await Assert.That(resourcesResult).Count().IsEqualTo(1);
    await Assert.That(promptsResult).Count().IsEqualTo(1);
  }

  [Test]
  public async Task EmptyNameList_ReturnsEmptyList()
  {
    var options = CreateOptions(new Dictionary<string, string?>
    {
      ["McpFiltering:Allowed:tools:0"] = "ToolA",
    });

    var strategy = new AppSettingsPrimitiveFilteringStrategy(options, NullLogger<AppSettingsPrimitiveFilteringStrategy>.Instance);

    var result = strategy.FilterPrimitives(McpPrimitiveType.Tool,
        Array.Empty<string>()).ToList();

    await Assert.That(result).Count().IsEqualTo(0);
  }

  [Test]
  public async Task SingleName_Allowed_ReturnsIt()
  {
    var options = CreateOptions(new Dictionary<string, string?>
    {
      ["McpFiltering:Allowed:tools:0"] = "OnlyTool",
    });

    var strategy = new AppSettingsPrimitiveFilteringStrategy(options, NullLogger<AppSettingsPrimitiveFilteringStrategy>.Instance);

    var result = strategy.FilterPrimitives(McpPrimitiveType.Tool,
        new[] { "OnlyTool" }).ToList();

    await Assert.That(result).Count().IsEqualTo(1);
    await Assert.That(result[0]).IsEqualTo("OnlyTool");
  }

  [Test]
  public async Task SingleName_Blocked_ReturnsEmpty()
  {
    var options = CreateOptions(new Dictionary<string, string?>
    {
      ["McpFiltering:Allowed:tools:0"] = "OtherTool",
    });

    var strategy = new AppSettingsPrimitiveFilteringStrategy(options, NullLogger<AppSettingsPrimitiveFilteringStrategy>.Instance);

    var result = strategy.FilterPrimitives(McpPrimitiveType.Tool,
        new[] { "MyTool" }).ToList();

    await Assert.That(result).Count().IsEqualTo(0);
  }

  [Test]
  public async Task EmptyConfigSection_ReturnsAll()
  {
    var options = CreateOptions();

    var strategy = new AppSettingsPrimitiveFilteringStrategy(options, NullLogger<AppSettingsPrimitiveFilteringStrategy>.Instance);

    var result = strategy.FilterPrimitives(McpPrimitiveType.Tool,
        new[] { "ToolA", "ToolB", "ToolC" }).ToList();

    await Assert.That(result).Count().IsEqualTo(3);
  }
}