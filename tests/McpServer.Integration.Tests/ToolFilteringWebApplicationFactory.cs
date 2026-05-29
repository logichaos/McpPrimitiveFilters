using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TUnit.Core.Interfaces;

namespace McpServer.Integration.Tests;

/// <summary>
/// A WebApplicationFactory that injects custom <c>Mcp:AllowedTools</c>
/// configuration for integration testing of the AppSettings tool filtering strategy.
/// </summary>
public class ToolFilteringWebApplicationFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    private readonly string[]? _allowedTools;

    public ToolFilteringWebApplicationFactory(string[]? allowedTools = null)
    {
        _allowedTools = allowedTools;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        if (_allowedTools is not null)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Mcp:AllowedTools:0"] = _allowedTools.Length > 0 ? _allowedTools[0] : null,
                    ["Mcp:AllowedTools:1"] = _allowedTools.Length > 1 ? _allowedTools[1] : null,
                    ["Mcp:AllowedTools:2"] = _allowedTools.Length > 2 ? _allowedTools[2] : null,
                    ["Mcp:AllowedTools:3"] = _allowedTools.Length > 3 ? _allowedTools[3] : null,
                    ["Mcp:AllowedTools:4"] = _allowedTools.Length > 4 ? _allowedTools[4] : null,
                });
            });
        }
    }

    public Task InitializeAsync()
    {
        _ = Server;
        return Task.CompletedTask;
    }
}
