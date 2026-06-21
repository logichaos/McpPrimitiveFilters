using Microsoft.AspNetCore.Hosting;

using TUnit.AspNetCore;

namespace McpServer.Integration.Tests.Infrastructure.Factories;

public class RateLimitingWebApplicationFactory : TestWebApplicationFactory<Program>
{
  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment("RateLimitTesting");
  }
}