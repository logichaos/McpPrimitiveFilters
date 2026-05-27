namespace McpServer.Integration.Tests;

public class Tests
{
  [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
  public required WebApplicationFactory WebApplicationFactory { get; init; }

  [Test]
  public async Task Test()
  {
    var client = WebApplicationFactory.CreateClient();

    var response = await client.GetAsync("/");

    var stringContent = await response.Content.ReadAsStringAsync();

    await Assert.That(stringContent).IsEqualTo("this is working");
  }
}