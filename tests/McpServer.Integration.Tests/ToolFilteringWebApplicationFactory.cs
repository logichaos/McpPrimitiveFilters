using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TUnit.Core.Interfaces;

namespace McpServer.Integration.Tests;

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
                var dict = new Dictionary<string, string?>();
                for (int i = 0; i < _allowedTools.Length; i++)
                    dict[$"McpFiltering:Allowed:tools:{i}"] = _allowedTools[i];
                config.AddInMemoryCollection(dict);
            });
        }
    }

    public Task InitializeAsync()
    {
        _ = Server;
        return Task.CompletedTask;
    }
}
