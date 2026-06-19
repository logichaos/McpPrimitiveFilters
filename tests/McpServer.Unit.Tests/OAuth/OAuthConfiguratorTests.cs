using McpServer.Infrastructure.OAuth;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Unit.Tests.OAuth;

public class OAuthConfiguratorTests
{
  private static readonly NullLoggerFactory LoggerFactory = NullLoggerFactory.Instance;

  [Test]
  public async Task InMemory_SetsAuthorityAndTokenValidation()
  {
    var configurator = new InMemoryOAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "InMemory",
      AuthorityUrl = "https://auth.example.com",
    };
    var oauth = new OAuthOptions { ServerUrl = "http://localhost:7071/" };

    configurator.Configure(options, scheme, oauth, LoggerFactory);

    await Assert.That(options.Authority).IsEqualTo("https://auth.example.com");
    await Assert.That(options.TokenValidationParameters.ValidateIssuer).IsTrue();
    await Assert.That(options.TokenValidationParameters.ValidateAudience).IsTrue();
    await Assert.That(options.TokenValidationParameters.ValidateLifetime).IsTrue();
    await Assert.That(options.TokenValidationParameters.ValidateIssuerSigningKey).IsTrue();
    await Assert.That(options.TokenValidationParameters.ValidIssuer)
        .IsEqualTo("https://auth.example.com");
    await Assert.That(options.TokenValidationParameters.ValidAudience)
        .IsEqualTo("http://localhost:7071/");
  }

  [Test]
  public async Task InMemory_UsesSchemeAudience_WhenProvided()
  {
    var configurator = new InMemoryOAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "InMemory",
      AuthorityUrl = "https://auth.example.com",
      Audience = "custom-audience",
    };

    configurator.Configure(options, scheme, new OAuthOptions { ServerUrl = "ignored" }, LoggerFactory);

    await Assert.That(options.TokenValidationParameters.ValidAudience)
        .IsEqualTo("custom-audience");
  }

  [Test]
  public async Task InMemory_UsesSchemeIssuer_WhenProvided()
  {
    var configurator = new InMemoryOAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "InMemory",
      AuthorityUrl = "https://auth.example.com",
      Issuer = "https://custom-issuer.example.com",
    };

    configurator.Configure(options, scheme, new OAuthOptions(), LoggerFactory);

    await Assert.That(options.TokenValidationParameters.ValidIssuer)
        .IsEqualTo("https://custom-issuer.example.com");
  }

  [Test]
  public async Task InMemory_DisablesBackchannelSsl_WhenRequested()
  {
    var configurator = new InMemoryOAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "InMemory",
      AuthorityUrl = "https://auth.example.com",
      DisableBackchannelSslValidation = true,
    };

    configurator.Configure(options, scheme, new OAuthOptions(), LoggerFactory);

    await Assert.That(options.BackchannelHttpHandler).IsNotNull();
    await Assert.That(options.BackchannelHttpHandler).IsTypeOf<SocketsHttpHandler>();
  }

  [Test]
  public async Task InMemory_DoesNotDisableBackchannelSsl_ByDefault()
  {
    var configurator = new InMemoryOAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "InMemory",
      AuthorityUrl = "https://auth.example.com",
      DisableBackchannelSslValidation = false,
    };

    configurator.Configure(options, scheme, new OAuthOptions(), LoggerFactory);

    await Assert.That(options.BackchannelHttpHandler).IsNull();
  }

  [Test]
  public async Task InMemory_Throws_WhenAuthorityUrlIsMissing()
  {
    var configurator = new InMemoryOAuthConfigurator();
    var scheme = new OAuthSchemeConfig { Type = "InMemory" };

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
    {
      configurator.Configure(new JwtBearerOptions(), scheme, new OAuthOptions(), LoggerFactory);
      return Task.CompletedTask;
    });
  }

  [Test]
  public async Task InMemory_SetsNameAndRoleClaimTypes()
  {
    var configurator = new InMemoryOAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "InMemory",
      AuthorityUrl = "https://auth.example.com",
    };

    configurator.Configure(options, scheme, new OAuthOptions(), LoggerFactory);

    await Assert.That(options.TokenValidationParameters.NameClaimType).IsEqualTo("name");
    await Assert.That(options.TokenValidationParameters.RoleClaimType).IsEqualTo("roles");
  }

  [Test]
  public async Task EntraId_ConstructsAuthority_FromTenantId()
  {
    var configurator = new EntraIdOAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "EntraId",
      TenantId = "my-tenant-id",
    };

    configurator.Configure(options, scheme, new OAuthOptions(), LoggerFactory);

    await Assert.That(options.Authority)
        .IsEqualTo("https://login.microsoftonline.com/my-tenant-id/v2.0");
  }

  [Test]
  public async Task EntraId_UsesCustomInstance_WhenProvided()
  {
    var configurator = new EntraIdOAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "EntraId",
      TenantId = "my-tenant",
      Instance = "https://login.microsoftonline.us/",
    };

    configurator.Configure(options, scheme, new OAuthOptions(), LoggerFactory);

    await Assert.That(options.Authority)
        .IsEqualTo("https://login.microsoftonline.us/my-tenant/v2.0");
  }

  [Test]
  public async Task EntraId_UsesClientIdAsAudience_WhenNoAudienceProvided()
  {
    var configurator = new EntraIdOAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "EntraId",
      TenantId = "my-tenant",
      ClientId = "api://my-app-id",
    };

    configurator.Configure(options, scheme, new OAuthOptions { ServerUrl = "http://fallback" }, LoggerFactory);

    await Assert.That(options.TokenValidationParameters.ValidAudience)
        .IsEqualTo("api://my-app-id");
  }

  [Test]
  public async Task EntraId_UsesAudience_OverClientId()
  {
    var configurator = new EntraIdOAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "EntraId",
      TenantId = "my-tenant",
      ClientId = "api://ignored",
      Audience = "api://explicit-audience",
    };

    configurator.Configure(options, scheme, new OAuthOptions(), LoggerFactory);

    await Assert.That(options.TokenValidationParameters.ValidAudience)
        .IsEqualTo("api://explicit-audience");
  }

  [Test]
  public async Task EntraId_Throws_WhenTenantIdIsMissing()
  {
    var configurator = new EntraIdOAuthConfigurator();
    var scheme = new OAuthSchemeConfig { Type = "EntraId" };

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
    {
      configurator.Configure(new JwtBearerOptions(), scheme, new OAuthOptions(), LoggerFactory);
      return Task.CompletedTask;
    });
  }

  [Test]
  public async Task Auth0_ConstructsAuthority_FromDomain()
  {
    var configurator = new Auth0OAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "Auth0",
      Domain = "my-tenant.us.auth0.com",
    };

    configurator.Configure(options, scheme, new OAuthOptions(), LoggerFactory);

    await Assert.That(options.Authority)
        .IsEqualTo("https://my-tenant.us.auth0.com/");
  }

  [Test]
  public async Task Auth0_TrimsTrailingSlash_FromDomain()
  {
    var configurator = new Auth0OAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "Auth0",
      Domain = "my-tenant.auth0.com/",
    };

    configurator.Configure(options, scheme, new OAuthOptions(), LoggerFactory);

    await Assert.That(options.Authority)
        .IsEqualTo("https://my-tenant.auth0.com/");
  }

  [Test]
  public async Task Auth0_UsesClientIdAsAudience_WhenNoAudienceProvided()
  {
    var configurator = new Auth0OAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "Auth0",
      Domain = "my-tenant.auth0.com",
      ClientId = "my-api-identifier",
    };

    configurator.Configure(options, scheme, new OAuthOptions { ServerUrl = "http://fallback" }, LoggerFactory);

    await Assert.That(options.TokenValidationParameters.ValidAudience)
        .IsEqualTo("my-api-identifier");
  }

  [Test]
  public async Task Auth0_UsesCustomRoleClaimType()
  {
    var configurator = new Auth0OAuthConfigurator();
    var options = new JwtBearerOptions();
    var scheme = new OAuthSchemeConfig
    {
      Type = "Auth0",
      Domain = "my-tenant.auth0.com",
    };

    configurator.Configure(options, scheme, new OAuthOptions(), LoggerFactory);

    await Assert.That(options.TokenValidationParameters.RoleClaimType)
        .IsEqualTo("https://schemas.example.com/roles");
  }

  [Test]
  public async Task Auth0_Throws_WhenDomainIsMissing()
  {
    var configurator = new Auth0OAuthConfigurator();
    var scheme = new OAuthSchemeConfig { Type = "Auth0" };

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
    {
      configurator.Configure(new JwtBearerOptions(), scheme, new OAuthOptions(), LoggerFactory);
      return Task.CompletedTask;
    });
  }

  [Test]
  public async Task InMemory_ProviderType_IsInMemory()
  {
    await Assert.That(new InMemoryOAuthConfigurator().ProviderType)
        .IsEqualTo("InMemory");
  }

  [Test]
  public async Task EntraId_ProviderType_IsEntraId()
  {
    await Assert.That(new EntraIdOAuthConfigurator().ProviderType)
        .IsEqualTo("EntraId");
  }

  [Test]
  public async Task Auth0_ProviderType_IsAuth0()
  {
    await Assert.That(new Auth0OAuthConfigurator().ProviderType)
        .IsEqualTo("Auth0");
  }
}