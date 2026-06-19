using Microsoft.Extensions.Logging.Abstractions;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

using McpServer.Integration.Tests.Infrastructure.Factories;

namespace McpServer.Integration.Tests.Basic;

public class TransportConfigurationTests
{
  [ClassDataSource<DefaultWebApplicationFactory>(Shared = SharedType.PerTestSession)]
  public required DefaultWebApplicationFactory HttpFactory { get; init; }

  // ── HTTP transport (default for "Testing" environment) ──

  private static async Task<McpClient> CreateMcpClientAsync(HttpClient httpClient)
  {
    var transport = new HttpClientTransport(
        new HttpClientTransportOptions
        {
          Endpoint = httpClient.BaseAddress!,
          TransportMode = HttpTransportMode.StreamableHttp,
        },
        httpClient,
        NullLoggerFactory.Instance,
        ownsHttpClient: false);

    return await McpClient.CreateAsync(transport);
  }

  [Test]
  public async Task HttpTransport_ServerStartsAndResponds()
  {
    var client = HttpFactory.CreateClient();
    var mcpClient = await CreateMcpClientAsync(client);

    var tools = await mcpClient.ListToolsAsync();
    await Assert.That(tools).IsNotNull();
    await Assert.That(tools.Count).IsPositive();
  }

  [Test]
  public async Task HttpTransport_RootEndpointResponds()
  {
    var client = HttpFactory.CreateClient();
    var response = await client.GetAsync("/");
    var body = await response.Content.ReadAsStringAsync();

    await Assert.That(body).IsEqualTo("this is working");
  }

  [Test]
  public async Task HttpTransport_McpEndpointAcceptsPost()
  {
    var client = HttpFactory.CreateClient();
    var mcpClient = await CreateMcpClientAsync(client);

    var result = await mcpClient.CallToolAsync("echo",
        new Dictionary<string, object?> { ["message"] = "transport-test" });

    var text = result.Content[0].ToString();
    await Assert.That(text).IsEqualTo("transport-test");
  }
}