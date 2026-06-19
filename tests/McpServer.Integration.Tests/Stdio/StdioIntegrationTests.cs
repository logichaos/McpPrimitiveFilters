using Microsoft.Extensions.Logging.Abstractions;

using ModelContextProtocol.Client;

using TUnit.Core.Interfaces;

namespace McpServer.Integration.Tests.Stdio;

public class StdioIntegrationTests : IAsyncInitializer
{
  private static readonly string McpServerDll = ResolveMcpServerDll();

  private McpClient? _sharedClient;

  private static string ResolveMcpServerDll()
  {
    var testDll = typeof(StdioIntegrationTests).Assembly.Location;
    var configuration = Path.GetFileName(Path.GetDirectoryName(testDll)!);
    var binDir = Path.GetFullPath(Path.Combine(testDll, "..", "..", ".."));
    return Path.Combine(binDir, "McpServer", configuration, "McpServer.dll");
  }

  public async Task InitializeAsync()
  {
    _sharedClient = await CreateStdioMcpClientAsync();
  }

  public async ValueTask DisposeAsync()
  {
    if (_sharedClient is not null)
      await _sharedClient.DisposeAsync();
  }

  private static async Task<McpClient> CreateStdioMcpClientAsync()
  {
    var mcpServerDir = Path.GetDirectoryName(McpServerDll)!;
    var transport = new StdioClientTransport(new StdioClientTransportOptions
    {
      Command = "dotnet",
      Arguments =
        [
            "exec",
                McpServerDll,
            ],
      WorkingDirectory = mcpServerDir,
      EnvironmentVariables = new Dictionary<string, string?>
      {
        ["ASPNETCORE_ENVIRONMENT"] = "Stdio",
      },
      ShutdownTimeout = TimeSpan.FromSeconds(10),
      StandardErrorLines = line => Console.Error.WriteLine($"[McpServer stderr] {line}"),
    });

    return await McpClient.CreateAsync(
        transport,
        loggerFactory: NullLoggerFactory.Instance);
  }

  [Test]
  public async Task StdioTransport_DiscoversTools()
  {
    var tools = await _sharedClient!.ListToolsAsync();
    var toolNames = tools.Select(t => t.Name).ToList();

    await Assert.That(toolNames).IsNotEmpty();
    await Assert.That(toolNames).Contains("get_timestamp");
    await Assert.That(toolNames).Contains("echo");
    await Assert.That(toolNames).Contains("get_random_number");
  }

  [Test]
  public async Task StdioTransport_EchoToolReturnsInput()
  {
    var result = await _sharedClient!.CallToolAsync("echo",
        new Dictionary<string, object?> { ["message"] = "stdio-test" });

    var text = result.Content[0].ToString();
    await Assert.That(text).IsEqualTo("stdio-test");
  }

  [Test]
  public async Task StdioTransport_GetTimestampReturnsIso8601()
  {
    var result = await _sharedClient!.CallToolAsync("get_timestamp");
    var text = result.Content[0].ToString();

    await Assert.That(DateTimeOffset.TryParse(text, out _)).IsTrue();
  }
}