using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TUnit.Core.Interfaces;

namespace McpServer.Integration.Tests;

public class ResourceFilteringWebApplicationFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    private readonly string[]? _allowedResources;

    public ResourceFilteringWebApplicationFactory(string[]? allowedResources = null)
    {
        _allowedResources = allowedResources;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        if (_allowedResources is not null)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                var dict = new Dictionary<string, string?>();
                for (int i = 0; i < _allowedResources.Length; i++)
                    dict[$"McpFiltering:Allowed:resources:{i}"] = _allowedResources[i];
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
