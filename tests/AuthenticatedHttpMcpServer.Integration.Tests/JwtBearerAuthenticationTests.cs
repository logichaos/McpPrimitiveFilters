using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AuthenticatedHttpMcpServer.Integration.Tests;

public class JwtBearerAuthenticationTests
{
    // Must match appsettings.Development.json ApiSettings:TokenValidation values.
    private const string ValidIssuer = "dotnet-user-jwts";
    private const string ValidAudience = "http://localhost:5105";

    // Signature is not verified in Development (RequireSignedTokens = false),
    // so any key produces an accepted token.
    private static readonly SymmetricSecurityKey TestKey =
        new(Encoding.UTF8.GetBytes("integration-test-signing-key-min-32-chars!!"));

    [ClassDataSource<RealAuthWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required RealAuthWebApplicationFactory Factory { get; init; }

    private static string CreateToken(
        string issuer = ValidIssuer,
        string audience = ValidAudience,
        IEnumerable<Claim>? claims = null,
        DateTime? expires = null) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims ?? [new Claim("sub", "test-user")]),
            Issuer = issuer,
            Audience = audience,
            Expires = expires ?? DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(TestKey, SecurityAlgorithms.HmacSha256),
        });

    [Test]
    public async Task NoAuthorizationHeader_Returns401()
    {
        var response = await Factory.CreateClient().GetAsync("/health");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task NonBearerScheme_Returns401()
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", "Basic dXNlcjpwYXNz");

        var response = await client.GetAsync("/health");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ValidToken_WithCorrectRole_Returns200()
    {
        var token = CreateToken(claims: [new Claim("role", "awesome"), new Claim("sub", "user1")]);
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var response = await client.GetAsync("/health");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task ValidToken_WithoutRole_Returns403()
    {
        var token = CreateToken(claims: [new Claim("sub", "user1")]);
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var response = await client.GetAsync("/health");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task WrongIssuer_Returns401()
    {
        var token = CreateToken(
            issuer: "https://evil.example.com",
            claims: [new Claim("role", "awesome"), new Claim("sub", "user1")]);
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var response = await client.GetAsync("/health");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task WrongAudience_Returns401()
    {
        var token = CreateToken(
            audience: "https://wrong-api.example.com",
            claims: [new Claim("role", "awesome"), new Claim("sub", "user1")]);
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var response = await client.GetAsync("/health");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ExpiredToken_Returns401()
    {
        var token = CreateToken(
            expires: DateTime.UtcNow.AddHours(-1),
            claims: [new Claim("role", "awesome"), new Claim("sub", "user1")]);
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var response = await client.GetAsync("/health");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
