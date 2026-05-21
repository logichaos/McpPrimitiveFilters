using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AuthenticatedHttpMcpServer.Tests;

public class Tests
{
  [ClassDataSource<TestWebApplicationFactory>(Shared = SharedType.PerTestSession)]
  public required TestWebApplicationFactory TestWebApplicationFactory { get; init; }

  [Test]
  public async Task HealthCheck_ReturnsHealthy()
  {
    var client = TestWebApplicationFactory.CreateClient();

    var response = await client.GetAsync("/health");

    var content = await response.Content.ReadAsStringAsync();
    await Assert.That(content).IsEqualTo("Healthy");
  }

  [Test]
  public async Task HealthCheck_WrongRole_ReturnsForbidden()
  {
    await using var factory = TestWebApplicationFactory.WithWebHostBuilder(b =>
      b.ConfigureTestServices(services =>
        services.PostConfigure<TestAuthHandlerOptions>(
          JwtBearerDefaults.AuthenticationScheme,
          opts => opts.Claims = [new Claim(ClaimTypes.Role, "wrong-role")])));

    var response = await factory.CreateClient().GetAsync("/health");

    await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
  }
}
