using Microsoft.AspNetCore.Hosting;

using TUnit.AspNetCore;

namespace McpServer.Integration.Tests.Infrastructure.Factories;

/// <summary>
/// Uses the "RateLimitTesting" environment: OAuth disabled, rate limiting
/// enabled with 1-second windows so counters reset between tests.
/// </summary>
public class RateLimitingWebApplicationFactory : TestWebApplicationFactory<Program>
{
  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment("RateLimitTesting");
  }
}