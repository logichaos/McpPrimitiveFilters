using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using ModelContextProtocol.AspNetCore.Authentication;

using TUnit.AspNetCore;

namespace McpServer.Integration.Tests.OAuth;

public sealed class OAuthWebApplicationFactory
    : TestWebApplicationFactory<Program>
{
  private static readonly Lock PortLock = new();
  private static int _nextPort = 18000;

  public string OAuthAuthority { get; private set; } = null!;

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    int port;
    lock (PortLock)
    {
      port = _nextPort++;
    }

    OAuthAuthority = $"https://localhost:{port}";

    builder.UseEnvironment("Development");

    builder.ConfigureAppConfiguration((_, config) =>
    {
      config.AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["Mcp:OAuth:EmbeddedOAuthServer:Enabled"] = "true",
        ["OAuthServer:Port"] = port.ToString(),
      });
    });

    builder.ConfigureServices(services =>
    {
      services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
      {
        options.Authority = OAuthAuthority;
        options.TokenValidationParameters.ValidIssuer = OAuthAuthority;
      });

      services.Configure<McpAuthenticationOptions>(McpAuthenticationDefaults.AuthenticationScheme, options =>
      {
        if (options.ResourceMetadata is not null)
        {
          options.ResourceMetadata.AuthorizationServers.Clear();
          options.ResourceMetadata.AuthorizationServers.Add(OAuthAuthority);
        }
      });
    });
  }
}
