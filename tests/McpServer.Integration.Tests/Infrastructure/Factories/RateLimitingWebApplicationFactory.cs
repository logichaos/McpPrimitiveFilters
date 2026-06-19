using Microsoft.AspNetCore.Hosting;

using TUnit.AspNetCore;

namespace McpServer.Integration.Tests.Infrastructure.Factories;

// 1-second rate limit windows so counters reset between tests.
// OAuth disabled to avoid authentication overhead.
public class RateLimitingWebApplicationFactory : TestWebApplicationFactory<Program>
{
  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment("RateLimitTesting");
  }
}