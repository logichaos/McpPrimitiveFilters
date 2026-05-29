using McpServer.Infrastructure.ToolFiltering;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace McpServer.Unit.Tests.ToolFiltering;

public class AppSettingsToolFilteringStrategyTests
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

    // ── AllowedTools contains a subset ──

    [Test]
    public async Task AllowedTools_ContainsSubset_ReturnsOnlyMatchingTools()
    {
        var config = CreateConfiguration("""
        {
            "Mcp": {
                "AllowedTools": ["GetRandomNumber", "Echo"]
            }
        }
        """);

        var strategy = new AppSettingsToolFilteringStrategy(config);
        var toolNames = new[] { "GetRandomNumber", "Echo", "GetTimestamp", "ListUsers", "GetServerStats" };

        var result = strategy.FilterTools(EmptyContext, toolNames).ToList();

        await Assert.That(result).Count().IsEqualTo(2);
        await Assert.That(result).Contains("GetRandomNumber");
        await Assert.That(result).Contains("Echo");
    }

    // ── AllowedTools is empty array ──

    [Test]
    public async Task AllowedTools_IsEmptyArray_ReturnsAllToolsUnchanged()
    {
        var config = CreateConfiguration("""
        {
            "Mcp": {
                "AllowedTools": []
            }
        }
        """);

        var strategy = new AppSettingsToolFilteringStrategy(config);
        var toolNames = new[] { "GetRandomNumber", "Echo", "GetTimestamp" };

        var result = strategy.FilterTools(EmptyContext, toolNames).ToList();

        await Assert.That(result).Count().IsEqualTo(3);
    }

    // ── AllowedTools key is missing ──

    [Test]
    public async Task AllowedTools_KeyMissing_ReturnsAllToolsUnchanged()
    {
        var config = CreateConfiguration("""
        {
            "Mcp": {
                "SomeOther": "value"
            }
        }
        """);

        var strategy = new AppSettingsToolFilteringStrategy(config);
        var toolNames = new[] { "GetRandomNumber", "Echo" };

        var result = strategy.FilterTools(EmptyContext, toolNames).ToList();

        await Assert.That(result).Count().IsEqualTo(2);
    }

    // ── AllowedTools references a non-existent tool ──

    [Test]
    public async Task AllowedTools_ReferencesNonExistentTool_ReturnsEmptyList()
    {
        var config = CreateConfiguration("""
        {
            "Mcp": {
                "AllowedTools": ["NonExistentTool"]
            }
        }
        """);

        var strategy = new AppSettingsToolFilteringStrategy(config);
        var toolNames = new[] { "GetRandomNumber", "Echo" };

        var result = strategy.FilterTools(EmptyContext, toolNames).ToList();

        await Assert.That(result).Count().IsEqualTo(0);
    }

    // ── AllowedTools name matching is case-insensitive ──

    [Test]
    public async Task AllowedTools_NameMatchingIsCaseInsensitive()
    {
        var config = CreateConfiguration("""
        {
            "Mcp": {
                "AllowedTools": ["getrandomnumber"]
            }
        }
        """);

        var strategy = new AppSettingsToolFilteringStrategy(config);
        var toolNames = new[] { "GetRandomNumber", "Echo" };

        var result = strategy.FilterTools(EmptyContext, toolNames).ToList();

        await Assert.That(result).Count().IsEqualTo(1);
        await Assert.That(result).Contains("GetRandomNumber");
    }
}
