using System.Security.Claims;
using System.Net.Http.Headers;

namespace AuthenticatedHttpMcpServer.Integration.Tests;

public class RateLimitingTests
{
    [ClassDataSource<TestWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required TestWebApplicationFactory TestWebApplicationFactory { get; init; }

    private HttpRequestMessage CreateHealthRequest()
    {
        var token = TestWebApplicationFactory.CreateTestToken(
        [
            new(ClaimTypes.Name, "test-user"),
            new(ClaimTypes.Role, "mcpcaller"),
            new(ClaimTypes.Role, "awesome"),
        ]);
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    [Test]
    public async Task RateLimitedEndpoint_SetsXRateLimitLimitHeader()
    {
        var client = TestWebApplicationFactory.CreateClient();

        var response = await client.SendAsync(CreateHealthRequest());

        await Assert.That(response.Headers.Contains("X-Rate-Limit-Limit")).IsTrue();
    }

    [Test]
    public async Task RateLimitedEndpoint_XRateLimitLimitHeader_IsNumeric()
    {
        var client = TestWebApplicationFactory.CreateClient();

        var response = await client.SendAsync(CreateHealthRequest());

        var headerValue = response.Headers.GetValues("X-Rate-Limit-Limit").FirstOrDefault();
        await Assert.That(int.TryParse(headerValue, out _)).IsTrue();
    }
}
