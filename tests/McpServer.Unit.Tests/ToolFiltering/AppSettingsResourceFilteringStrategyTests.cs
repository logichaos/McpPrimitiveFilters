using McpServer.Infrastructure.ToolFiltering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace McpServer.Unit.Tests.ToolFiltering;

public class AppSettingsResourceFilteringStrategyTests
{
    private static DefaultHttpContext EmptyContext => new();

    private static IConfiguration CreateConfiguration(string? json)
    {
        var builder = new ConfigurationBuilder();
        if (json is not null)
        {
            builder.AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)));
        }
        return builder.Build();
    }

    [Test]
    public async Task AllowedResources_ContainsSubset_ReturnsOnlyMatchingResources()
    {
        var config = CreateConfiguration("""
        {
            "Mcp": {
                "AllowedResources": ["Server Info", "Current Time"]
            }
        }
        """);

        var strategy = new AppSettingsResourceFilteringStrategy(config);
        var resourceNames = new[] { "Server Info", "City Weather", "Process Info", "Current Time" };

        var result = strategy.FilterResources(EmptyContext, resourceNames).ToList();

        await Assert.That(result).Count().IsEqualTo(2);
        await Assert.That(result).Contains("Server Info");
        await Assert.That(result).Contains("Current Time");
    }

    [Test]
    public async Task AllowedResources_IsEmptyArray_ReturnsAllResourcesUnchanged()
    {
        var config = CreateConfiguration("""
        {
            "Mcp": {
                "AllowedResources": []
            }
        }
        """);

        var strategy = new AppSettingsResourceFilteringStrategy(config);
        var resourceNames = new[] { "Server Info", "Process Info", "Current Time" };

        var result = strategy.FilterResources(EmptyContext, resourceNames).ToList();

        await Assert.That(result).Count().IsEqualTo(3);
    }

    [Test]
    public async Task AllowedResources_KeyMissing_ReturnsAllResourcesUnchanged()
    {
        var config = CreateConfiguration("""
        {
            "Mcp": {
                "SomeOther": "value"
            }
        }
        """);

        var strategy = new AppSettingsResourceFilteringStrategy(config);
        var resourceNames = new[] { "Server Info", "Process Info" };

        var result = strategy.FilterResources(EmptyContext, resourceNames).ToList();

        await Assert.That(result).Count().IsEqualTo(2);
    }

    [Test]
    public async Task AllowedResources_ReferencesNonExistentResource_ReturnsEmptyList()
    {
        var config = CreateConfiguration("""
        {
            "Mcp": {
                "AllowedResources": ["NonExistentResource"]
            }
        }
        """);

        var strategy = new AppSettingsResourceFilteringStrategy(config);
        var resourceNames = new[] { "Server Info", "Process Info" };

        var result = strategy.FilterResources(EmptyContext, resourceNames).ToList();

        await Assert.That(result).Count().IsEqualTo(0);
    }

    [Test]
    public async Task AllowedResources_NameMatchingIsCaseInsensitive()
    {
        var config = CreateConfiguration("""
        {
            "Mcp": {
                "AllowedResources": ["server info"]
            }
        }
        """);

        var strategy = new AppSettingsResourceFilteringStrategy(config);
        var resourceNames = new[] { "Server Info", "Process Info" };

        var result = strategy.FilterResources(EmptyContext, resourceNames).ToList();

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result).Contains("Server Info");
    }
}
