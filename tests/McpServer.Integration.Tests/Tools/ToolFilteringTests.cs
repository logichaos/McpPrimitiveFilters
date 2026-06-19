using Microsoft.Extensions.Logging.Abstractions;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

using McpServer.Integration.Tests.Infrastructure.Factories;

namespace McpServer.Integration.Tests.Tools;

public class ToolFilteringTests
{
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
  public async Task AllowedTools_Configured_ListToolsReturnsOnlyAllowed()
  {
    await using var factory = new ToolFilteringWebApplicationFactory(
        ["get_random_number", "echo"]);

    var mcpServer = factory.CreateClient();
    var mcpClient = await CreateMcpClientAsync(mcpServer);

    var tools = await mcpClient.ListToolsAsync();
    var toolNames = tools.Select(t => t.Name).ToList();

    await Assert.That(toolNames).Count().IsEqualTo(2);
    await Assert.That(toolNames).Contains("get_random_number");
    await Assert.That(toolNames).Contains("echo");
    await Assert.That(toolNames).DoesNotContain("get_timestamp");
    await Assert.That(toolNames).DoesNotContain("list_users");
    await Assert.That(toolNames).DoesNotContain("get_server_stats");
  }

  [Test]
  public async Task NoFilteringConfigured_ListToolsReturnsAllTools()
  {
    // Uses the standard WebApplicationFactory with no AllowedTools config
    await using var factory = new ToolFilteringWebApplicationFactory(); // null = no config
                                                                        // Default to "Testing" environment which has no AllowedTools

    var mcpServer = factory.CreateClient();
    var mcpClient = await CreateMcpClientAsync(mcpServer);

    var tools = await mcpClient.ListToolsAsync();
    var toolNames = tools.Select(t => t.Name).ToList();

    await Assert.That(toolNames).Count().IsEqualTo(5);
    await Assert.That(toolNames).Contains("get_random_number");
    await Assert.That(toolNames).Contains("get_timestamp");
    await Assert.That(toolNames).Contains("echo");
    await Assert.That(toolNames).Contains("list_users");
    await Assert.That(toolNames).Contains("get_server_stats");
  }

  [Test]
  public async Task AllowedTools_AllowedTool_InvocationSucceeds()
  {
    await using var factory = new ToolFilteringWebApplicationFactory(
        ["get_random_number"]);

    var mcpServer = factory.CreateClient();
    var mcpClient = await CreateMcpClientAsync(mcpServer);

    var result = await mcpClient.CallToolAsync("get_random_number",
        new Dictionary<string, object?> { ["min"] = 1, ["max"] = 2 });

    // Successful tool calls may have IsError = false or null
    await Assert.That(result.IsError).IsNotEqualTo(true);
  }

  [Test]
  public async Task AllowedTools_BlockedTool_InvocationReturnsError()
  {
    await using var factory = new ToolFilteringWebApplicationFactory(
        ["get_random_number"]);

    var mcpServer = factory.CreateClient();
    var mcpClient = await CreateMcpClientAsync(mcpServer);

    // echo is not in the allowlist — should be blocked
    var result = await mcpClient.CallToolAsync("echo",
        new Dictionary<string, object?> { ["message"] = "test" });

    await Assert.That(result.IsError).IsTrue();
    var text = result.Content.OfType<TextContentBlock>().First().Text;
    await Assert.That(text).Contains("not authorized");
  }
}
