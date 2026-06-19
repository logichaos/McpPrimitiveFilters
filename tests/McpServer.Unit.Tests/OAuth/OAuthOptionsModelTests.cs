using McpServer.Infrastructure.OAuth;

namespace McpServer.Unit.Tests.OAuth;

public class OAuthOptionsModelTests
{
  [Test]
  public async Task OAuthOptions_DefaultScheme_IsEmptyString()
  {
    var options = new OAuthOptions();
    await Assert.That(options.DefaultScheme).IsEqualTo("");
  }

  [Test]
  public async Task OAuthOptions_ServerUrl_IsEmptyString()
  {
    var options = new OAuthOptions();
    await Assert.That(options.ServerUrl).IsEqualTo("");
  }

  [Test]
  public async Task OAuthOptions_Schemes_IsEmptyDictionary()
  {
    var options = new OAuthOptions();
    await Assert.That(options.Schemes).IsNotNull();
    await Assert.That(options.Schemes.Count).IsEqualTo(0);
  }

  [Test]
  public async Task OAuthOptions_ScopesSupported_IsEmptyArray()
  {
    var options = new OAuthOptions();
    await Assert.That(options.ScopesSupported).IsNotNull();
    await Assert.That(options.ScopesSupported.Length).IsEqualTo(0);
  }

  [Test]
  public async Task OAuthOptions_Resource_IsNullByDefault()
  {
    var options = new OAuthOptions();
    await Assert.That(options.Resource).IsNull();
  }

  [Test]
  public async Task OAuthOptions_ResourceDocumentation_IsNullByDefault()
  {
    var options = new OAuthOptions();
    await Assert.That(options.ResourceDocumentation).IsNull();
  }

  [Test]
  public async Task OAuthOptions_SectionName_IsCorrect()
  {
    await Assert.That(OAuthOptions.SectionName).IsEqualTo("Mcp:OAuth");
  }

  [Test]
  public async Task OAuthSchemeConfig_Enabled_IsFalseByDefault()
  {
    var config = new OAuthSchemeConfig();
    await Assert.That(config.Enabled).IsFalse();
  }

  [Test]
  public async Task OAuthSchemeConfig_Type_IsEmptyString()
  {
    var config = new OAuthSchemeConfig();
    await Assert.That(config.Type).IsEqualTo("");
  }

  [Test]
  public async Task OAuthSchemeConfig_DisplayName_IsNullByDefault()
  {
    var config = new OAuthSchemeConfig();
    await Assert.That(config.DisplayName).IsNull();
  }

  [Test]
  public async Task OAuthSchemeConfig_AuthorityUrl_IsNullByDefault()
  {
    var config = new OAuthSchemeConfig();
    await Assert.That(config.AuthorityUrl).IsNull();
  }

  [Test]
  public async Task OAuthSchemeConfig_Audience_IsNullByDefault()
  {
    var config = new OAuthSchemeConfig();
    await Assert.That(config.Audience).IsNull();
  }

  [Test]
  public async Task OAuthSchemeConfig_Issuer_IsNullByDefault()
  {
    var config = new OAuthSchemeConfig();
    await Assert.That(config.Issuer).IsNull();
  }

  [Test]
  public async Task OAuthSchemeConfig_DisableBackchannelSslValidation_IsFalseByDefault()
  {
    var config = new OAuthSchemeConfig();
    await Assert.That(config.DisableBackchannelSslValidation).IsFalse();
  }

  [Test]
  public async Task OAuthSchemeConfig_TenantId_IsNullByDefault()
  {
    var config = new OAuthSchemeConfig();
    await Assert.That(config.TenantId).IsNull();
  }

  [Test]
  public async Task OAuthSchemeConfig_Instance_IsNullByDefault()
  {
    var config = new OAuthSchemeConfig();
    await Assert.That(config.Instance).IsNull();
  }

  [Test]
  public async Task OAuthSchemeConfig_Domain_IsNullByDefault()
  {
    var config = new OAuthSchemeConfig();
    await Assert.That(config.Domain).IsNull();
  }

  [Test]
  public async Task OAuthSchemeConfig_ClientId_IsNullByDefault()
  {
    var config = new OAuthSchemeConfig();
    await Assert.That(config.ClientId).IsNull();
  }
}