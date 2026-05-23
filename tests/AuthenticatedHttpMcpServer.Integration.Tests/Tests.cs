using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AuthenticatedHttpMcpServer.Integration.Tests;

public class Tests
{
    [ClassDataSource<TestWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required TestWebApplicationFactory TestWebApplicationFactory { get; init; }

    [Test]
    public async Task HealthCheck_ReturnsHealthy()
    {
        var client = TestWebApplicationFactory.CreateClient();
        var token = TestWebApplicationFactory.CreateTestToken(
        [
            new(ClaimTypes.Name, "test-user"),
            new(ClaimTypes.Role, "mcpcaller"),
            new(ClaimTypes.Role, "awesome"),
        ]);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var response = await client.GetAsync("/health");

        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).IsEqualTo("Healthy");
    }

    [Test]
    public async Task HealthCheck_WrongRole_ReturnsForbidden()
    {
        var client = TestWebApplicationFactory.CreateClient();
        var token = TestWebApplicationFactory.CreateTestToken(
        [
            new(ClaimTypes.Name, "test-user"),
            new(ClaimTypes.Role, "wrong-role"),
        ]);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        var response = await client.GetAsync("/health");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task HealthCheck_NoToken_ReturnsUnauthorized()
    {
        var response = await TestWebApplicationFactory.CreateClient().GetAsync("/health");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
