using System.Net;
using System.Security.Claims;

namespace AuthenticatedHttpMcpServer.Integration.Tests;

/// <summary>
/// Tests the JWT Bearer authentication and authorization pipeline using
/// locally-signed test tokens (the TestWebApplicationFactory reconfigures
/// JwtBearer to accept HS256 tokens signed with a known test key).
/// </summary>
public class JwtBearerAuthenticationTests
{
    [ClassDataSource<TestWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required TestWebApplicationFactory Factory { get; init; }

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
        var client = Factory.CreateClient();
        var token = TestWebApplicationFactory.CreateTestToken(
        [
            new(ClaimTypes.Name, "test-user"),
            new(ClaimTypes.Role, "mcpcaller"),
            new(ClaimTypes.Role, "awesome"),
        ]);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var response = await client.GetAsync("/health");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task ValidToken_NoRoles_Returns403()
    {
        var client = Factory.CreateClient();
        var token = TestWebApplicationFactory.CreateTestToken(
        [
            new(ClaimTypes.Name, "test-user"),
        ]);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var response = await client.GetAsync("/health");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task ExpiredToken_Returns401()
    {
        var client = Factory.CreateClient();
        var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
        var expiredToken = handler.CreateToken(new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new(ClaimTypes.Name, "test-user"),
                new(ClaimTypes.Role, "mcpcaller"),
                new(ClaimTypes.Role, "awesome"),
            ]),
            Expires = DateTime.UtcNow.AddHours(-1),
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                TestWebApplicationFactory.TestSigningKey,
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256),
        });
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {expiredToken}");

        var response = await client.GetAsync("/health");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
