using Microsoft.Extensions.Logging.Abstractions;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

using McpServer.Integration.Tests.Infrastructure.Factories;

namespace McpServer.Integration.Tests.Logging;

public class LoggingCapabilityTests
{
  [ClassDataSource<DefaultWebApplicationFactory>(Shared = SharedType.PerTestSession)]
  public required DefaultWebApplicationFactory Factory { get; init; }

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
  public async Task Server_AdvertisesLoggingCapability()
  {
    var client = Factory.CreateClient();
    var mcpClient = await CreateMcpClientAsync(client);

    var logging = mcpClient.ServerCapabilities?.Logging;

    await Assert.That(logging).IsNotNull();
  }

  [Test]
  public async Task SetLoggingLevel_Succeeds_WithValidLevel()
  {
    var client = Factory.CreateClient();
    var mcpClient = await CreateMcpClientAsync(client);

    await mcpClient.SetLoggingLevelAsync(LoggingLevel.Debug);
  }

  [Test]
  public async Task SetLoggingLevel_Succeeds_WithWarningLevel()
  {
    var client = Factory.CreateClient();
    var mcpClient = await CreateMcpClientAsync(client);

    await mcpClient.SetLoggingLevelAsync(LoggingLevel.Warning);
  }

  [Test]
  public async Task SetLoggingLevel_Succeeds_WithErrorLevel()
  {
    var client = Factory.CreateClient();
    var mcpClient = await CreateMcpClientAsync(client);

    await mcpClient.SetLoggingLevelAsync(LoggingLevel.Error);
  }

  [Test]
  public async Task SetLoggingLevel_Succeeds_WithAllValidLevels()
  {
    var client = Factory.CreateClient();
    var mcpClient = await CreateMcpClientAsync(client);

    var levels = new[]
    {
            LoggingLevel.Debug,
            LoggingLevel.Info,
            LoggingLevel.Notice,
            LoggingLevel.Warning,
            LoggingLevel.Error,
            LoggingLevel.Critical,
            LoggingLevel.Alert,
            LoggingLevel.Emergency,
        };

    foreach (var level in levels)
    {
      await mcpClient.SetLoggingLevelAsync(level);
    }
  }
}