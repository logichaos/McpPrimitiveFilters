using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using TUnit.AspNetCore;

namespace McpServer.Integration.Tests.OAuth;

public sealed class OAuthWebApplicationFactory
    : TestWebApplicationFactory<Program>
{
  [ClassDataSource<OAuthServerWebApplicationFactory>(Shared = SharedType.PerTestSession)]
  public required OAuthServerWebApplicationFactory OAuthServer { get; init; }

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment("Development");

    var oauthBaseAddress = OAuthServer.Server.BaseAddress!;
    var authority = oauthBaseAddress.ToString().TrimEnd('/');

    builder.ConfigureServices(services =>
    {
      services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
          {
          options.Authority = authority;
          options.Backchannel = CreateBackchannel();
          options.TokenValidationParameters = new TokenValidationParameters
          {
            ValidAudience = "http://localhost",
            ValidIssuer = authority,
            NameClaimType = "name",
            RoleClaimType = "roles"
          };
        });
    });
  }

  private static HttpClient CreateBackchannel()
  {
    var handler = new SocketsHttpHandler
    {
      SslOptions = { RemoteCertificateValidationCallback = (_, _, _, _) => true }
    };
    return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
  }
}