using McpPrimitiveFilters;

namespace McpPrimitiveFilters.Unit.Tests;

public class McpPrimitiveFiltersOptionsTests
{
    [Test]
    public async Task DefaultOptions_HasExpectedValues()
    {
        var options = new McpPrimitiveFiltersOptions();

        await Assert.That(options.UseBuiltinAppSettingsFilteringStrategy).IsTrue();
        await Assert.That(options.UseBuiltinOAuthClaimsFilteringStrategy).IsTrue();
        await Assert.That(options.FilterTools).IsTrue();
        await Assert.That(options.FilterResources).IsTrue();
        await Assert.That(options.FilterPrompts).IsTrue();
    }

    [Test]
    public async Task Options_CanDisableAllFiltering()
    {
        var options = new McpPrimitiveFiltersOptions
        {
            UseBuiltinAppSettingsFilteringStrategy = false,
            UseBuiltinOAuthClaimsFilteringStrategy = false,
            FilterTools = false,
            FilterResources = false,
            FilterPrompts = false
        };

        await Assert.That(options.UseBuiltinAppSettingsFilteringStrategy).IsFalse();
        await Assert.That(options.UseBuiltinOAuthClaimsFilteringStrategy).IsFalse();
        await Assert.That(options.FilterTools).IsFalse();
        await Assert.That(options.FilterResources).IsFalse();
        await Assert.That(options.FilterPrompts).IsFalse();
    }

    [Test]
    public async Task Options_CanDisablePromptsOnly()
    {
        var options = new McpPrimitiveFiltersOptions
        {
            FilterPrompts = false
        };

        await Assert.That(options.FilterPrompts).IsFalse();
        await Assert.That(options.FilterTools).IsTrue();
    }
}
