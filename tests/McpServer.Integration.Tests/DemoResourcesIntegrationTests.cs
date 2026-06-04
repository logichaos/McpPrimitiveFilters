using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace McpServer.Integration.Tests;

public class DemoResourcesIntegrationTests
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

    [Test]
    public async Task ListResources_ReturnsDirectResources()
    {
        var client = Factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var resources = await mcpClient.ListResourcesAsync();
        var resourceNames = resources.Select(r => r.Name).ToList();

        await Assert.That(resourceNames).Contains("Server Info");
        await Assert.That(resourceNames).Contains("Process Info");
    }

    [Test]
    public async Task ListResourceTemplates_ReturnsTemplatedResources()
    {
        var client = Factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var templates = await mcpClient.ListResourceTemplatesAsync();
        var templateNames = templates.Select(t => t.Name).ToList();

        await Assert.That(templateNames).Contains("City Weather");
        await Assert.That(templateNames).Contains("Current Time");
    }

    [Test]
    public async Task ReadResource_ServerInfo_ReturnsSystemInfoJson()
    {
        var client = Factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var result = await mcpClient.ReadResourceAsync("server://info");
        var text = ((TextResourceContents)result.Contents[0]).Text;

        await Assert.That(text).IsNotNull().And.IsNotEmpty();
        await Assert.That(text).Contains("MachineName").And.Contains("OsDescription");
    }

    [Test]
    public async Task ReadResource_ProcessInfo_ReturnsProcessData()
    {
        var client = Factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var result = await mcpClient.ReadResourceAsync("system://process-info");
        var text = ((TextResourceContents)result.Contents[0]).Text;

        await Assert.That(text).IsNotNull().And.IsNotEmpty();
        await Assert.That(text).Contains("ProcessId").And.Contains("ProcessName");
    }

    [Test]
    public async Task ReadResource_Weather_ReturnsWeatherForCity()
    {
        var client = Factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var result = await mcpClient.ReadResourceAsync("weather://London");
        var text = ((TextResourceContents)result.Contents[0]).Text;

        await Assert.That(text).IsNotNull().And.IsNotEmpty();
        await Assert.That(text).Contains("\"City\"").And.Contains("London");
    }

    [Test]
    public async Task ReadResource_Weather_SameCityReturnsDeterministicResult()
    {
        var client = Factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var result1 = await mcpClient.ReadResourceAsync("weather://Oslo");
        var text1 = ((TextResourceContents)result1.Contents[0]).Text;

        var result2 = await mcpClient.ReadResourceAsync("weather://Oslo");
        var text2 = ((TextResourceContents)result2.Contents[0]).Text;

        await Assert.That(text1).IsEqualTo(text2);
    }

    [Test]
    public async Task ReadResource_CurrentTime_Iso_ReturnsIso8601()
    {
        var client = Factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var result = await mcpClient.ReadResourceAsync("time://iso");
        var text = ((TextResourceContents)result.Contents[0]).Text;

        await Assert.That(DateTimeOffset.TryParse(text, out _)).IsTrue();
    }

    [Test]
    public async Task ReadResource_CurrentTime_Unix_ReturnsInteger()
    {
        var client = Factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var result = await mcpClient.ReadResourceAsync("time://unix");
        var text = ((TextResourceContents)result.Contents[0]).Text;

        await Assert.That(long.TryParse(text, out var ts)).IsTrue();
        await Assert.That(ts).IsGreaterThan(0);
    }

    [Test]
    public async Task ReadResource_CurrentTime_Rfc_ReturnsRfc1123()
    {
        var client = Factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var result = await mcpClient.ReadResourceAsync("time://rfc");
        var text = ((TextResourceContents)result.Contents[0]).Text;

        await Assert.That(DateTimeOffset.TryParse(text, out _)).IsTrue();
    }

    [Test]
    public async Task ReadResource_CurrentTime_Ticks_ReturnsLongInteger()
    {
        var client = Factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var result = await mcpClient.ReadResourceAsync("time://ticks");
        var text = ((TextResourceContents)result.Contents[0]).Text;

        await Assert.That(long.TryParse(text, out var ticks)).IsTrue();
        await Assert.That(ticks).IsGreaterThan(0);
    }

    [Test]
    public async Task ReadResource_CurrentTime_UnknownFormat_ThrowsMcpException()
    {
        var client = Factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        await Assert.ThrowsAsync(async () => await mcpClient.ReadResourceAsync("time://invalid"));
    }

    [Test]
    public async Task ReadResource_UnknownUri_ThrowsResourceNotFound()
    {
        var client = Factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        await Assert.ThrowsAsync(async () => await mcpClient.ReadResourceAsync("unknown://resource"));
    }
}
