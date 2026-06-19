using System.Net;
using System.Text;

namespace McpServer.Integration.Tests.OAuth;

/// <summary>
/// Integration tests that verify the OAuth pipeline using the
/// OAuthWebApplicationFactory (shared factory approach with OAuth enabled).
///
/// For full end-to-end OAuth flow tests (authorization redirect, PKCE,
/// token refresh, scope selection), see OAuthFlowTests which uses the
/// KestrelInMemoryTest approach.
/// </summary>
public class OAuthHttpTests
{
  [ClassDataSource<OAuthWebApplicationFactory>(Shared = SharedType.PerTestSession)]
  public required OAuthWebApplicationFactory Factory { get; init; }

  // ──────────────────────────────────────────────────────────────
  // Smoke: app starts successfully with OAuth enabled
  // ──────────────────────────────────────────────────────────────

  [Test]
  public async Task AppStarts_WithOAuthEnabled()
  {
    var client = Factory.CreateClient();

    // The root endpoint is not behind auth, so it should still work
    var response = await client.GetAsync("/");
    await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
  }

  // ──────────────────────────────────────────────────────────────
  // 401 without authentication
  // ──────────────────────────────────────────────────────────────

  [Test]
  public async Task McpEndpoint_Returns401_WithoutToken()
  {
    var client = Factory.CreateClient();

    using var content = new StringContent("{}", Encoding.UTF8, "application/json");
    var response = await client.PostAsync("/", content);

    await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
  }

  [Test]
  public async Task McpEndpoint_ReturnsWwwAuthenticateHeader()
  {
    var client = Factory.CreateClient();

    using var content = new StringContent("{}", Encoding.UTF8, "application/json");
    var response = await client.PostAsync("/", content);

    await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    await Assert.That(response.Headers.Contains("WWW-Authenticate")).IsTrue();
    var header = response.Headers.GetValues("WWW-Authenticate").First();
    await Assert.That(header).Contains("resource_metadata");
  }

  [Test]
  public async Task McpEndpoint_Returns401_WithInvalidToken()
  {
    var client = Factory.CreateClient();
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token-value");

    using var content = new StringContent("{}", Encoding.UTF8, "application/json");
    var response = await client.PostAsync("/", content);

    await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
  }

  // ──────────────────────────────────────────────────────────────
  // Protected Resource Metadata endpoint
  // ──────────────────────────────────────────────────────────────

  [Test]
  public async Task ProtectedResourceMetadata_Returns200()
  {
    var client = Factory.CreateClient();

    var response = await client.GetAsync("/.well-known/oauth-protected-resource");

    await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
  }

  [Test]
  public async Task ProtectedResourceMetadata_ContainsAuthorizationServer()
  {
    var client = Factory.CreateClient();

    var response = await client.GetAsync("/.well-known/oauth-protected-resource");
    var body = await response.Content.ReadAsStringAsync();

    var oauthAuthority = Factory.OAuthServer.Server.BaseAddress!.ToString().TrimEnd('/');
    await Assert.That(body).Contains(oauthAuthority);
  }

  [Test]
  public async Task ProtectedResourceMetadata_ContainsScopesSupported()
  {
    var client = Factory.CreateClient();

    var response = await client.GetAsync("/.well-known/oauth-protected-resource");
    var body = await response.Content.ReadAsStringAsync();

    await Assert.That(body).Contains("mcp:tools");
  }

  [Test]
  public async Task ProtectedResourceMetadata_IsValidJson()
  {
    var client = Factory.CreateClient();

    var response = await client.GetAsync("/.well-known/oauth-protected-resource");
    var body = await response.Content.ReadAsStringAsync();

    await Assert.That(body).IsNotNull();
    await Assert.That(body.Trim()).StartsWith("{");
    await Assert.That(body.Trim()).EndsWith("}");
  }
}