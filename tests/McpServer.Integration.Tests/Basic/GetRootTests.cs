using McpServer.Integration.Tests.Infrastructure.Factories;

namespace McpServer.Integration.Tests.Basic;

public class GetRootTests
{
  [ClassDataSource<DefaultWebApplicationFactory>(Shared = SharedType.PerTestSession)]
  public required DefaultWebApplicationFactory WebApplicationFactory { get; init; }

  [Test]
  public async Task Test()
  {
    var client = WebApplicationFactory.CreateClient();

    var response = await client.GetAsync("/");

    var stringContent = await response.Content.ReadAsStringAsync();

    await Assert.That(stringContent).IsEqualTo("this is working");
  }
}