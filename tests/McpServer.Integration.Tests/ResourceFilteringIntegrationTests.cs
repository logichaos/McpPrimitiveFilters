using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace McpServer.Integration.Tests;

public class ResourceFilteringIntegrationTests
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
    public async Task NoFilteringConfigured_ListResourcesReturnsAllDirectResources()
    {
        await using var factory = new ResourceFilteringWebApplicationFactory();

        var client = factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var resources = await mcpClient.ListResourcesAsync();
        var resourceNames = resources.Select(r => r.Name).ToList();

        await Assert.That(resourceNames).Count().IsEqualTo(2);
        await Assert.That(resourceNames).Contains("Server Info");
        await Assert.That(resourceNames).Contains("Process Info");
    }

    [Test]
    public async Task NoFilteringConfigured_ListResourceTemplatesReturnsAllTemplates()
    {
        await using var factory = new ResourceFilteringWebApplicationFactory();

        var client = factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var templates = await mcpClient.ListResourceTemplatesAsync();
        var templateNames = templates.Select(t => t.Name).ToList();

        await Assert.That(templateNames).Count().IsEqualTo(2);
        await Assert.That(templateNames).Contains("City Weather");
        await Assert.That(templateNames).Contains("Current Time");
    }

    [Test]
    public async Task AllowedResources_Configured_ListResourcesReturnsOnlyAllowed()
    {
        await using var factory = new ResourceFilteringWebApplicationFactory(
            ["Server Info"]);

        var client = factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var resources = await mcpClient.ListResourcesAsync();
        var resourceNames = resources.Select(r => r.Name).ToList();

        await Assert.That(resourceNames).Count().IsEqualTo(1);
        await Assert.That(resourceNames).Contains("Server Info");
        await Assert.That(resourceNames).DoesNotContain("Process Info");
    }

    [Test]
    public async Task AllowedResources_Configured_ListResourceTemplatesReturnsOnlyAllowed()
    {
        await using var factory = new ResourceFilteringWebApplicationFactory(
            ["Current Time"]);

        var client = factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var templates = await mcpClient.ListResourceTemplatesAsync();
        var templateNames = templates.Select(t => t.Name).ToList();

        await Assert.That(templateNames).Count().IsEqualTo(1);
        await Assert.That(templateNames).Contains("Current Time");
        await Assert.That(templateNames).DoesNotContain("City Weather");
    }

    [Test]
    public async Task AllowedResources_AllowedDirectResource_ReadSucceeds()
    {
        await using var factory = new ResourceFilteringWebApplicationFactory(
            ["Server Info"]);

        var client = factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var result = await mcpClient.ReadResourceAsync("server://info");

        await Assert.That(result.Contents).IsNotEmpty();
        var text = ((TextResourceContents)result.Contents[0]).Text;
        await Assert.That(text).IsNotNull().And.IsNotEmpty();
    }

    [Test]
    public async Task AllowedResources_BlockedDirectResource_ReadReturnsErrorContent()
    {
        await using var factory = new ResourceFilteringWebApplicationFactory(
            ["Server Info"]);

        var client = factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var result = await mcpClient.ReadResourceAsync("system://process-info");

        await Assert.That(result.Contents).IsNotEmpty();
        var text = ((TextResourceContents)result.Contents[0]).Text;
        await Assert.That(text).Contains("not authorized");
    }

    [Test]
    public async Task AllowedResources_AllowedTemplateResource_ReadSucceeds()
    {
        await using var factory = new ResourceFilteringWebApplicationFactory(
            ["Current Time"]);

        var client = factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var result = await mcpClient.ReadResourceAsync("time://iso");

        await Assert.That(result.Contents).IsNotEmpty();
        var text = ((TextResourceContents)result.Contents[0]).Text;
        await Assert.That(text).IsNotNull().And.IsNotEmpty();
    }

    [Test]
    public async Task AllowedResources_BlockedTemplateResource_ReadReturnsErrorContent()
    {
        await using var factory = new ResourceFilteringWebApplicationFactory(
            ["Current Time"]);

        var client = factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var result = await mcpClient.ReadResourceAsync("weather://London");

        await Assert.That(result.Contents).IsNotEmpty();
        var text = ((TextResourceContents)result.Contents[0]).Text;
        await Assert.That(text).Contains("not authorized");
    }

    [Test]
    public async Task AllowedResources_EmptyList_IsNoOp()
    {
        await using var factory = new ResourceFilteringWebApplicationFactory([]);

        var client = factory.CreateClient();
        var mcpClient = await CreateMcpClientAsync(client);

        var resources = await mcpClient.ListResourcesAsync();
        var templates = await mcpClient.ListResourceTemplatesAsync();

        await Assert.That(resources).IsNotEmpty();
        await Assert.That(templates).IsNotEmpty();
    }
}
