using System.Net;

using McpServer.Integration.Tests.Infrastructure.Factories;

namespace McpServer.Integration.Tests.RateLimiting;

public class HttpRateLimitingTests
{
  [ClassDataSource<RateLimitingWebApplicationFactory>(Shared = SharedType.None)]
  public required RateLimitingWebApplicationFactory Factory { get; init; }

  private const int FixedPermitLimit = 10;

  [Test]
  public async Task RootEndpoint_ReturnsExpectedBody()
  {
    var client = Factory.CreateClient();

    var response = await client.GetAsync("/");
    var body = await response.Content.ReadAsStringAsync();

    await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    await Assert.That(body).IsEqualTo("this is working");
  }

  [Test]
  public async Task RootEndpoint_ReturnsRateLimitHeader()
  {
    var client = Factory.CreateClient();

    var response = await client.GetAsync("/");

    await Assert.That(response.Headers.Contains("X-Rate-Limit-Limit")).IsTrue();
    await Assert.That(response.Headers.GetValues("X-Rate-Limit-Limit").First())
        .IsEqualTo(FixedPermitLimit.ToString());
  }

  [Test]
  public async Task RootEndpoint_Returns429_WhenRateLimitExceeded()
  {
    var client = Factory.CreateClient();

    for (int i = 0; i < FixedPermitLimit; i++)
      await client.GetAsync("/");

    var response = await client.GetAsync("/");

    await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
  }

  [Test]
  public async Task RootEndpoint_429_IncludesRetryAfterHeader()
  {
    var client = Factory.CreateClient();

    for (int i = 0; i < FixedPermitLimit; i++)
      await client.GetAsync("/");

    var response = await client.GetAsync("/");

    await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
    await Assert.That(response.Headers.Contains("Retry-After")).IsTrue();

    var retryAfter = int.Parse(response.Headers.GetValues("Retry-After").First());
    await Assert.That(retryAfter).IsPositive();
  }

  [Test]
  public async Task RootEndpoint_429_ReturnsPlainTextBody()
  {
    var client = Factory.CreateClient();

    for (int i = 0; i < FixedPermitLimit; i++)
      await client.GetAsync("/");

    var response = await client.GetAsync("/");
    var body = await response.Content.ReadAsStringAsync();

    await Assert.That(response.Content.Headers.ContentType!.MediaType)
        .IsEqualTo("text/plain");
    await Assert.That(body).Contains("Rate limit reached")
        .And.Contains("Retry after");
  }
}