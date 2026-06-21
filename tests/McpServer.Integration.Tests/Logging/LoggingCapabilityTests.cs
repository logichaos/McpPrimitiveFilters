using System.Collections.Concurrent;
using System.Text.Json;

using Microsoft.Extensions.Logging.Abstractions;

using ModelContextProtocol;
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

  private static (ConcurrentBag<LoggingMessageNotificationParams> Logs, IAsyncDisposable Subscription)
      CaptureLogNotifications(McpClient mcpClient)
  {
    var received = new ConcurrentBag<LoggingMessageNotificationParams>();
    var subscription = mcpClient.RegisterNotificationHandler(
        NotificationMethods.LoggingMessageNotification,
        (notification, ct) =>
        {
          var logParams = JsonSerializer.Deserialize<LoggingMessageNotificationParams>(
              notification.Params!, McpJsonUtilities.DefaultOptions);
          if (logParams is not null)
            received.Add(logParams);
          return default;
        });
    return (received, subscription);
  }

  [Test]
  public async Task Echo_SendsLogNotification_WithMessageLength()
  {
    var client = Factory.CreateClient();
    var mcpClient = await CreateMcpClientAsync(client);

    var (receivedLogs, subscription) = CaptureLogNotifications(mcpClient);
    await using var _ = subscription;

    await mcpClient.CallToolAsync("echo",
        new Dictionary<string, object?> { ["message"] = "Hello, Logs!" });

    await Task.Delay(500);

    await Assert.That(receivedLogs).IsNotEmpty();
    var log = receivedLogs.First();
    await Assert.That(log.Logger).IsEqualTo("Tools");
    await Assert.That(log.Level).IsEqualTo(LoggingLevel.Info);
    await Assert.That(log.Data.ToString()).Contains("echo")
        .And.Contains("12");
  }

  [Test]
  public async Task GetRandomNumber_SendsLogNotification_WithRangeAndResult()
  {
    var client = Factory.CreateClient();
    var mcpClient = await CreateMcpClientAsync(client);

    var (receivedLogs, subscription) = CaptureLogNotifications(mcpClient);
    await using var _ = subscription;

    await mcpClient.CallToolAsync("get_random_number",
        new Dictionary<string, object?> { ["min"] = 10, ["max"] = 20 });

    await Task.Delay(500);

    await Assert.That(receivedLogs).IsNotEmpty();
    var log = receivedLogs.First();
    await Assert.That(log.Logger).IsEqualTo("Tools");
    await Assert.That(log.Level).IsEqualTo(LoggingLevel.Info);
    await Assert.That(log.Data.ToString())
        .Contains("get_random_number")
        .And.Contains("Min").And.Contains("10")
        .And.Contains("Max").And.Contains("20");
  }

  [Test]
  public async Task LogNotification_IncludesCorrectLogLevel()
  {
    var client = Factory.CreateClient();
    var mcpClient = await CreateMcpClientAsync(client);

    var (receivedLogs, subscription) = CaptureLogNotifications(mcpClient);
    await using var _ = subscription;

    await mcpClient.CallToolAsync("echo",
        new Dictionary<string, object?> { ["message"] = "level-check" });

    await Task.Delay(500);

    await Assert.That(receivedLogs).IsNotEmpty();
    await Assert.That(receivedLogs.First().Level).IsEqualTo(LoggingLevel.Info);
  }

  [Test]
  public async Task LogNotifications_ArriveForMultipleToolInvocations()
  {
    var client = Factory.CreateClient();
    var mcpClient = await CreateMcpClientAsync(client);

    var (receivedLogs, subscription) = CaptureLogNotifications(mcpClient);
    await using var _ = subscription;

    await mcpClient.CallToolAsync("echo",
        new Dictionary<string, object?> { ["message"] = "first" });
    await mcpClient.CallToolAsync("get_random_number",
        new Dictionary<string, object?> { ["min"] = 0, ["max"] = 10 });

    await Task.Delay(500);

    var logs = receivedLogs.ToArray();
    await Assert.That(logs).Count().IsEqualTo(2);
    await Assert.That(logs.Any(l => l.Data.ToString().Contains("echo"))).IsTrue();
    await Assert.That(logs.Any(l => l.Data.ToString().Contains("get_random_number"))).IsTrue();
  }
}