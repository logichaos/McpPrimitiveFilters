using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

using TUnit.AspNetCore;

namespace McpServer.Integration.Tests.Infrastructure.Factories;

public class ToolFilteringWebApplicationFactory : TestWebApplicationFactory<Program>
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
}