using System.Net;

namespace AuthenticatedHttpMcpServer.Integration.Tests;

public class ApiKeyAuthenticationTests
{
  private const string ApiKeyHeader = "Ocp-Apim-Subscription-Key";
  private const string ValidApiKey = "Lifetime Subscription";

  [ClassDataSource<ApiKeyServerFixture>(Shared = SharedType.PerTestSession)]
  public required ApiKeyServerFixture Fixture { get; init; }

  [Test]
  public async Task ValidApiKey_Returns200()
  {
    var client = Fixture.CreateClient();
    client.DefaultRequestHeaders.Add(ApiKeyHeader, ValidApiKey);

    var response = await client.GetAsync("/api-key-protected");

    await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
  }

  [Test]
  public async Task InvalidApiKey_Returns401()
  {
    var client = Fixture.CreateClient();
    client.DefaultRequestHeaders.Add(ApiKeyHeader, "wrong-key");

    var response = await client.GetAsync("/api-key-protected");

    await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
  }

  [Test]
  public async Task MissingApiKeyHeader_Returns401()
  {
    var response = await Fixture.CreateClient().GetAsync("/api-key-protected");

    await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
  }

  [Test]
  public async Task WrongHeader_Returns401()
  {
    var client = Fixture.CreateClient();
    client.DefaultRequestHeaders.Add("wrong-header", ValidApiKey);

    var response = await client.GetAsync("/api-key-protected");

    await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
  }
}
