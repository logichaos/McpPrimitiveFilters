using McpServer.Infrastructure.OAuth;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Unit.Tests.OAuth;

public class OAuthConfiguratorEventTests
{
  private static readonly NullLoggerFactory LoggerFactory = NullLoggerFactory.Instance;

  [Test]
  public async Task InMemory_SetsOnTokenValidatedEvent()
  {
    var configurator = new InMemoryOAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "InMemory",
      AuthorityUrl = "https://auth.example.com",
    };

    configurator.Configure(options, scheme, new OAuthOptions(), LoggerFactory);

    await Assert.That(options.Events).IsNotNull();
    await Assert.That(options.Events!.OnTokenValidated).IsNotNull();
  }

  [Test]
  public async Task InMemory_SetsOnAuthenticationFailedEvent()
  {
    var configurator = new InMemoryOAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "InMemory",
      AuthorityUrl = "https://auth.example.com",
    };

    configurator.Configure(options, scheme, new OAuthOptions(), LoggerFactory);

    await Assert.That(options.Events!.OnAuthenticationFailed).IsNotNull();
  }

  [Test]
  public async Task EntraId_SetsOnTokenValidatedEvent()
  {
    var configurator = new EntraIdOAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "EntraId",
      TenantId = "my-tenant",
    };

    configurator.Configure(options, scheme, new OAuthOptions(), LoggerFactory);

    await Assert.That(options.Events).IsNotNull();
    await Assert.That(options.Events!.OnTokenValidated).IsNotNull();
  }

  [Test]
  public async Task EntraId_SetsOnAuthenticationFailedEvent()
  {
    var configurator = new EntraIdOAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "EntraId",
      TenantId = "my-tenant",
    };

    configurator.Configure(options, scheme, new OAuthOptions(), LoggerFactory);

    await Assert.That(options.Events!.OnAuthenticationFailed).IsNotNull();
  }

  [Test]
  public async Task EntraId_DisablesBackchannelSsl_WhenRequested()
  {
    var configurator = new EntraIdOAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "EntraId",
      TenantId = "my-tenant",
      DisableBackchannelSslValidation = true,
    };

    configurator.Configure(options, scheme, new OAuthOptions(), LoggerFactory);

    await Assert.That(options.BackchannelHttpHandler).IsNotNull();
    await Assert.That(options.BackchannelHttpHandler)
        .IsTypeOf<SocketsHttpHandler>();
  }

  [Test]
  public async Task EntraId_DoesNotDisableBackchannelSsl_ByDefault()
  {
    var configurator = new EntraIdOAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "EntraId",
      TenantId = "my-tenant",
    };

    configurator.Configure(options, scheme, new OAuthOptions(), LoggerFactory);

    await Assert.That(options.BackchannelHttpHandler).IsNull();
  }

  [Test]
  public async Task Auth0_SetsOnTokenValidatedEvent()
  {
    var configurator = new Auth0OAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "Auth0",
      Domain = "my-tenant.auth0.com",
    };

    configurator.Configure(options, scheme, new OAuthOptions(), LoggerFactory);

    await Assert.That(options.Events).IsNotNull();
    await Assert.That(options.Events!.OnTokenValidated).IsNotNull();
  }

  [Test]
  public async Task Auth0_SetsOnAuthenticationFailedEvent()
  {
    var configurator = new Auth0OAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "Auth0",
      Domain = "my-tenant.auth0.com",
    };

    configurator.Configure(options, scheme, new OAuthOptions(), LoggerFactory);

    await Assert.That(options.Events!.OnAuthenticationFailed).IsNotNull();
  }

  [Test]
  public async Task Auth0_DisablesBackchannelSsl_WhenRequested()
  {
    var configurator = new Auth0OAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "Auth0",
      Domain = "my-tenant.auth0.com",
      DisableBackchannelSslValidation = true,
    };

    configurator.Configure(options, scheme, new OAuthOptions(), LoggerFactory);

    await Assert.That(options.BackchannelHttpHandler).IsNotNull();
    await Assert.That(options.BackchannelHttpHandler)
        .IsTypeOf<SocketsHttpHandler>();
  }

  [Test]
  public async Task Auth0_DoesNotDisableBackchannelSsl_ByDefault()
  {
    var configurator = new Auth0OAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "Auth0",
      Domain = "my-tenant.auth0.com",
    };

    configurator.Configure(options, scheme, new OAuthOptions(), LoggerFactory);

    await Assert.That(options.BackchannelHttpHandler).IsNull();
  }
}