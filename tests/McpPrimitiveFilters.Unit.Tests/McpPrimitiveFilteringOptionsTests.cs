using McpPrimitiveFilters;

namespace McpServer.Unit.Tests.McpPrimitiveFilters;

public class McpPrimitiveFiltersOptionsTests
{
    [Test]
    public async Task DefaultOptions_HasExpectedValues()
    {
        var options = new McpPrimitiveFiltersOptions();

        await Assert.That(options.AppSettingsEnabled).IsTrue();
        await Assert.That(options.OAuthClaimsEnabled).IsTrue();
        await Assert.That(options.FilterTools).IsTrue();
        await Assert.That(options.FilterResources).IsTrue();
        await Assert.That(options.FilterPrompts).IsFalse();
    }

    [Test]
    public async Task Options_CanDisableAllFiltering()
    {
        var options = new McpPrimitiveFiltersOptions
        {
            AppSettingsEnabled = false,
            OAuthClaimsEnabled = false,
            FilterTools = false,
            FilterResources = false
        };

        await Assert.That(options.AppSettingsEnabled).IsFalse();
        await Assert.That(options.OAuthClaimsEnabled).IsFalse();
        await Assert.That(options.FilterTools).IsFalse();
        await Assert.That(options.FilterResources).IsFalse();
    }

    [Test]
    public async Task Options_EnablePrompts()
    {
        var options = new McpPrimitiveFiltersOptions
        {
            FilterPrompts = true
        };

        await Assert.That(options.FilterPrompts).IsTrue();
    }
}
