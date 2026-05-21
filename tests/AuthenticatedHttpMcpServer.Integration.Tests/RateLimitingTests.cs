namespace AuthenticatedHttpMcpServer.Integration.Tests;

public class RateLimitingTests
{
    [ClassDataSource<TestWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required TestWebApplicationFactory TestWebApplicationFactory { get; init; }

    [Test]
    public async Task RateLimitedEndpoint_SetsXRateLimitLimitHeader()
    {
        var client = TestWebApplicationFactory.CreateClient();

        var response = await client.GetAsync("/health");

        await Assert.That(response.Headers.Contains("X-Rate-Limit-Limit")).IsTrue();
    }

    [Test]
    public async Task RateLimitedEndpoint_XRateLimitLimitHeader_IsNumeric()
    {
        var client = TestWebApplicationFactory.CreateClient();

        var response = await client.GetAsync("/health");

        var headerValue = response.Headers.GetValues("X-Rate-Limit-Limit").FirstOrDefault();
        await Assert.That(int.TryParse(headerValue, out _)).IsTrue();
    }
}
