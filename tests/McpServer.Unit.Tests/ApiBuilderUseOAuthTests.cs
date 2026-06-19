using McpServer.Infrastructure;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Unit.Tests;

public class ApiBuilderUseOAuthTests
{
  [Test]
  public async Task UseOAuth_WhenOAuthConfigured_DoesNotThrow()
  {
    var builder = WebApplication.CreateBuilder(Array.Empty<string>());
    builder.Services.AddSingleton(new ApiBuilder.OAuthMarker());
    builder.Services.AddAuthorization();
    builder.Services.AddCors();
    builder.Services.AddAuthenticationCore();
    var app = builder.Build();

    var result = ApiBuilder.UseOAuth(app);

    await Assert.That(result).IsNotNull();
  }

  [Test]
  public async Task UseOAuth_WhenOAuthConfigured_ReturnsSameApp()
  {
    var builder = WebApplication.CreateBuilder(Array.Empty<string>());
    builder.Services.AddSingleton(new ApiBuilder.OAuthMarker());
    builder.Services.AddAuthorization();
    builder.Services.AddCors();
    builder.Services.AddAuthenticationCore();
    var app = builder.Build();

    var result = ApiBuilder.UseOAuth(app);

    await Assert.That(result).IsEqualTo(app);
  }

  [Test]
  public async Task UseOAuth_WhenNotConfigured_ReturnsAppUnchanged()
  {
    var builder = WebApplication.CreateBuilder(Array.Empty<string>());
    var app = builder.Build();

    var result = ApiBuilder.UseOAuth(app);

    await Assert.That(result).IsEqualTo(app);
  }

  [Test]
  public async Task UseOAuth_WhenNotConfigured_DoesNotThrow()
  {
    var builder = WebApplication.CreateBuilder(Array.Empty<string>());
    var app = builder.Build();

    var result = ApiBuilder.UseOAuth(app);

    await Assert.That(result).IsEqualTo(app);
  }

  [Test]
  public async Task UseOAuth_WhenNotConfigured_NoAuthServicesNeeded()
  {
    var builder = WebApplication.CreateBuilder(Array.Empty<string>());
    var app = builder.Build();

    var result = ApiBuilder.UseOAuth(app);

    await Assert.That(result).IsNotNull();
  }
}