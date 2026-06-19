using System.Net;

using McpServer.Integration.Tests.Infrastructure.KestrelInMemory;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;

namespace McpServer.Integration.Tests.OAuth;

public abstract class OAuthTestBase : KestrelInMemoryTest, IAsyncDisposable
{
  protected const string McpServerUrl = "http://localhost:5000";
  protected const string OAuthServerUrl = "https://localhost:7029";

  protected readonly CancellationTokenSource TestCts = new();
  protected readonly ModelContextProtocol.TestOAuthServer.Program TestOAuthServer;
  private readonly Task _testOAuthRunTask;

  protected OAuthTestBase(bool configureMcpMetadata = true)
  {
    SocketsHttpHandler.AllowAutoRedirect = false;
    SocketsHttpHandler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;

    TestOAuthServer = new ModelContextProtocol.TestOAuthServer.Program();
    _testOAuthRunTask = TestOAuthServer.RunServerAsync(
        kestrelTransport: KestrelInMemoryTransport,
        cancellationToken: TestCts.Token);

    Builder.Services.AddAuthentication(options =>
    {
      options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
      options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
      options.Backchannel = HttpClient;
      options.Authority = OAuthServerUrl;
      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidAudience = McpServerUrl,
        ValidIssuer = OAuthServerUrl,
        NameClaimType = "name",
        RoleClaimType = "roles"
      };
    })
    .AddMcp(options =>
    {
      if (configureMcpMetadata)
      {
        options.ResourceMetadata = new ProtectedResourceMetadata
        {
          AuthorizationServers = { OAuthServerUrl },
          ScopesSupported = ["mcp:tools"]
        };
      }
    });

    Builder.Services.AddAuthorization();
    Builder.Services.AddMcpServer().WithHttpTransport();
  }

  public async ValueTask DisposeAsync()
  {
    TestCts.Cancel();
    try
    {
      await _testOAuthRunTask;
    }
    catch (OperationCanceledException)
    {
    }
    finally
    {
      TestCts.Dispose();
    }
  }

  protected async Task<WebApplication> StartMcpServerAsync(
      string path = "",
      string? authScheme = null,
      Action<WebApplication>? configureMiddleware = null)
  {
    await TestOAuthServer.ServerStarted.WaitAsync(TestContext.Current!.Execution.CancellationToken);

    Builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
    {
      options.TokenValidationParameters.ValidAudience = $"{McpServerUrl}{path}";
    });

    var app = Builder.Build();

    configureMiddleware?.Invoke(app);

    app.MapMcp(path).RequireAuthorization(new AuthorizeAttribute
    {
      AuthenticationSchemes = authScheme
    });
    await app.StartAsync(TestContext.Current.Execution.CancellationToken);
    return app;
  }

  protected async Task<string?> HandleAuthorizationUrlAsync(
      Uri authorizationUri,
      Uri redirectUri,
      CancellationToken cancellationToken)
  {
    using var redirectResponse = await HttpClient.GetAsync(authorizationUri, cancellationToken);
    await Assert.That(redirectResponse.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
    var location = redirectResponse.Headers.Location;

    if (location is not null && !string.IsNullOrEmpty(location.Query))
    {
      var queryParams = QueryHelpers.ParseQuery(location.Query);
      return queryParams["code"];
    }

    return null;
  }
}