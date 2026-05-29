using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;

namespace McpServer.Integration.Tests;

public class DemoToolsIntegrationTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory Factory { get; init; }

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

    // ──────────────────────────────────────────────────────────
    // Tool discovery
    // ──────────────────────────────────────────────────────────

    [Test]
    public async Task McpClient_DiscoversAllDemoTools()
    {
        var client = Factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var tools = await mcpClient.ListToolsAsync();
        var toolNames = tools.Select(t => t.Name).ToList();

        await Assert.That(toolNames).Contains("get_timestamp");
        await Assert.That(toolNames).Contains("echo");
        await Assert.That(toolNames).Contains("list_users");
        await Assert.That(toolNames).Contains("get_server_stats");
        await Assert.That(toolNames).Contains("get_random_number"); // existing tool still present
    }

    // ──────────────────────────────────────────────────────────
    // GetTimestamp
    // ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetTimestamp_ReturnsIso8601()
    {
        var client = Factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var result = await mcpClient.CallToolAsync("get_timestamp");
        var text = result.Content[0].ToString();

        await Assert.That(DateTimeOffset.TryParse(text, out _)).IsTrue();
    }

    // ──────────────────────────────────────────────────────────
    // Echo
    // ──────────────────────────────────────────────────────────

    [Test]
    public async Task Echo_ReturnsInputMessage()
    {
        var client = Factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var result = await mcpClient.CallToolAsync("echo", new Dictionary<string, object?>
        {
            ["message"] = "Hello, Integration Test!"
        });
        var text = result.Content[0].ToString();

        await Assert.That(text).IsEqualTo("Hello, Integration Test!");
    }

    // ──────────────────────────────────────────────────────────
    // ListUsers
    // ──────────────────────────────────────────────────────────

    [Test]
    public async Task ListUsers_ReturnsUserList()
    {
        var client = Factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var result = await mcpClient.CallToolAsync("list_users");
        var text = result.Content[0].ToString();

        await Assert.That(text).IsNotNull().And.IsNotEmpty();
        // Should contain known demo usernames
        await Assert.That(text).Contains("alice").And.Contains("admin");
    }

    // ──────────────────────────────────────────────────────────
    // GetServerStats
    // ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetServerStats_ReturnsStats()
    {
        var client = Factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var result = await mcpClient.CallToolAsync("get_server_stats");
        var text = result.Content[0].ToString();

        await Assert.That(text).IsNotNull().And.IsNotEmpty();
        await Assert.That(text).Contains("uptime").And.Contains("requestCount");
    }
}
