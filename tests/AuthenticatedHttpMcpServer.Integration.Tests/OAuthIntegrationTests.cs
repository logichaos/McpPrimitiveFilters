using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TUnit.Core.Interfaces;

namespace AuthenticatedHttpMcpServer.Integration.Tests;

/// <summary>
/// Test fixture that starts the TestOAuthServer in-process for end-to-end OAuth testing.
/// </summary>
public sealed class OAuthServerFixture : IAsyncInitializer, IAsyncDisposable
{
    private const int Port = 17029;
    public string ServerUrl => $"http://localhost:{Port}";

    private ModelContextProtocol.TestOAuthServer.Program? _oauthServer;
    private CancellationTokenSource? _oauthCts;

    public OAuthServerFixture()
    {
    }

    public async Task InitializeAsync()
    {
        _oauthCts = new CancellationTokenSource();
        _oauthServer = new ModelContextProtocol.TestOAuthServer.Program(Port, useHttps: false)
        {
            ValidResources = ["http://localhost:5105", "http://localhost:5105/mcp"],
        };

        _ = Task.Run(() => _oauthServer.RunServerAsync(cancellationToken: _oauthCts.Token));
        await _oauthServer.ServerStarted;

        Console.WriteLine($"TestOAuthServer started at {ServerUrl}");
    }

    /// <summary>
    /// Runs the full PKCE-based OAuth flow and returns an access token.
    /// </summary>
    public async Task<string> GetAccessToken(string clientId, string clientSecret)
    {
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = ComputeCodeChallenge(codeVerifier);

        using var httpClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false  // Don't follow the redirect to the callback URL
        });

        var authUrl = $"{ServerUrl}/authorize?client_id={clientId}&response_type=code" +
                      $"&code_challenge={codeChallenge}&code_challenge_method=S256" +
                      $"&redirect_uri=http://localhost:1179/callback" +
                      $"&scope=mcp:tools&resource={Uri.EscapeDataString("http://localhost:5105")}";

        var authResponse = await httpClient.GetAsync(authUrl);
        var redirectUrl = authResponse.Headers.Location?.ToString()
                          ?? throw new InvalidOperationException("No redirect URL");
        var code = ExtractQueryParam(redirectUrl, "code")
                   ?? throw new InvalidOperationException("No authorization code");

        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["code_verifier"] = codeVerifier,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = "http://localhost:1179/callback",
            ["resource"] = "http://localhost:5105",
        });

        var tokenResponse = await httpClient.PostAsync($"{ServerUrl}/token", tokenRequest);
        tokenResponse.EnsureSuccessStatusCode();

        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
        using var tokenDoc = JsonDocument.Parse(tokenJson);
        return tokenDoc.RootElement.GetProperty("access_token").GetString()
               ?? throw new InvalidOperationException("No access_token");
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        Random.Shared.NextBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string ComputeCodeChallenge(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string? ExtractQueryParam(string url, string paramName)
    {
        var uri = new Uri(url);
        var query = uri.Query.TrimStart('?');
        foreach (var pair in query.Split('&'))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && parts[0] == paramName)
                return Uri.UnescapeDataString(parts[1]);
        }
        return null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_oauthCts is not null)
        {
            await _oauthCts.CancelAsync();
            _oauthCts.Dispose();
        }
    }
}

/// <summary>
/// End-to-end tests that verify the TestOAuthServer (PKCE flow, token issuance,
/// client registration, JWKS and metadata endpoints).
/// </summary>
public class OAuthIntegrationTests
{
    [ClassDataSource<OAuthServerFixture>(Shared = SharedType.PerTestSession)]
    public required OAuthServerFixture Fixture { get; init; }

    [Test]
    public async Task GetToken_DemoClient_Succeeds()
    {
        var accessToken = await Fixture.GetAccessToken("demo-client", "demo-secret");
        Console.WriteLine($"✅ Demo token: {accessToken[..50]}...");
        await Assert.That(accessToken).IsNotNull();
    }

    [Test]
    public async Task GetToken_AliceClient_Succeeds()
    {
        var accessToken = await Fixture.GetAccessToken("alice-client", "alice-secret");
        Console.WriteLine($"✅ Alice token: {accessToken[..50]}...");
        await Assert.That(accessToken).IsNotNull();
    }

    [Test]
    public async Task GetToken_BobClient_Succeeds()
    {
        var accessToken = await Fixture.GetAccessToken("bob-client", "bob-secret");
        Console.WriteLine($"✅ Bob token: {accessToken[..50]}...");
        await Assert.That(accessToken).IsNotNull();
    }

    [Test]
    public async Task GetToken_InvalidClient_Returns401()
    {
        using var httpClient = new HttpClient();

        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = "fake-code",
            ["code_verifier"] = "fake-verifier",
            ["client_id"] = "nonexistent-client",
            ["client_secret"] = "wrong-secret",
            ["redirect_uri"] = "http://localhost:1179/callback",
            ["resource"] = "http://localhost:5105",
        });

        var response = await httpClient.PostAsync($"{Fixture.ServerUrl}/token", tokenRequest);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task RegistrationEndpoint_CreatesNewClient()
    {
        using var httpClient = new HttpClient();

        var registrationPayload = JsonSerializer.Serialize(new
        {
            redirect_uris = new[] { "http://localhost:5173/callback" },
            client_name = "test-dynamic-client",
        });

        var content = new StringContent(registrationPayload, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync($"{Fixture.ServerUrl}/register", content);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var clientId = doc.RootElement.GetProperty("client_id").GetString();
        await Assert.That(clientId).IsNotNull();
        Console.WriteLine($"✅ Dynamically registered client: {clientId}");
    }

    [Test]
    public async Task JwksEndpoint_ReturnsPublicKey()
    {
        using var httpClient = new HttpClient();

        var response = await httpClient.GetAsync($"{Fixture.ServerUrl}/.well-known/jwks.json");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var keys = doc.RootElement.GetProperty("keys");
        await Assert.That(keys.GetArrayLength()).IsPositive();
    }

    [Test]
    public async Task MetadataEndpoint_ReturnsOAuthConfig()
    {
        using var httpClient = new HttpClient();

        var response = await httpClient.GetAsync($"{Fixture.ServerUrl}/.well-known/oauth-authorization-server");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        await Assert.That(doc.RootElement.GetProperty("issuer").GetString()).IsEqualTo(Fixture.ServerUrl);
    }
}
