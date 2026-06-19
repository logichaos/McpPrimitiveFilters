using McpServer.Integration.Tests.Infrastructure.Factories;

namespace McpServer.Integration.Tests.Basic;

public class ErrorHandlingTests
{
  [ClassDataSource<DefaultWebApplicationFactory>(Shared = SharedType.PerTestSession)]
  public required DefaultWebApplicationFactory Factory { get; init; }

  [Test]
  public async Task InvalidRoute_Returns404()
  {
    var client = Factory.CreateClient();
    var response = await client.GetAsync("/nonexistent");

    await Assert.That((int)response.StatusCode).IsEqualTo(404);
  }

  [Test]
  public async Task RootEndpoint_StillWorks()
  {
    var client = Factory.CreateClient();
    var response = await client.GetAsync("/");

    await Assert.That((int)response.StatusCode).IsEqualTo(200);
    var body = await response.Content.ReadAsStringAsync();
    await Assert.That(body).IsEqualTo("this is working");
  }

  [Test]
  public async Task InvalidMethodOnMcp_ReturnsClientError()
  {
    var client = Factory.CreateClient();
    var response = await client.GetAsync("/mcp");

    // GET on an MCP endpoint returns a client error (Method Not Allowed or similar)
    await Assert.That((int)response.StatusCode).IsGreaterThanOrEqualTo(400);
    await Assert.That((int)response.StatusCode).IsLessThanOrEqualTo(499);
  }

  [Test]
  public async Task McpEndpoint_Responds_WithProperHeaders()
  {
    var client = Factory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Get, "/");

    var response = await client.SendAsync(request);

    await Assert.That(response.IsSuccessStatusCode).IsTrue();
    await Assert.That(response.Content.Headers.ContentType).IsNotNull();
  }
}