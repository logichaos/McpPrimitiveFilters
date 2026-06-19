using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using TUnit.AspNetCore;

namespace McpServer.Integration.Tests.OAuth;

public class OAuthServerWebApplicationFactory
    : TestWebApplicationFactory<ModelContextProtocol.TestOAuthServer.Program>
{
  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment("Testing");
  }

  public ModelContextProtocol.TestOAuthServer.OAuthServerState GetState() =>
      Server.Services.GetRequiredService<ModelContextProtocol.TestOAuthServer.OAuthServerState>();
}