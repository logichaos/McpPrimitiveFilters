using System.ComponentModel;
using McpServer.Infrastructure.Compliance;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

public record DemoUser(string Username, string Role);

public record ServerStats(
    [property: SensitiveData] string Uptime,
    int RequestCount);

internal static partial class ToolLogMessages
{
    [LoggerMessage(Level = LogLevel.Information,
        Message = "ListUsers: returned {Count} users")]
    public static partial void LogListUsers(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Server stats: Uptime={Uptime}, Requests={RequestCount}")]
    public static partial void LogServerStats(
        ILogger logger,
        [SensitiveData] string uptime,
        int requestCount);
}

public class RandomNumberTools
{
  private readonly ILogger<RandomNumberTools> _logger;

  public RandomNumberTools(ILogger<RandomNumberTools> logger)
  {
    _logger = logger;
  }

  [McpServerTool]
  [Description("Generates a random number between the specified minimum and maximum values.")]
  public int GetRandomNumber(
    [Description("Minimum value (inclusive)")] int min = 0,
    [Description("Maximum value (exclusive)")] int max = 100)
  {
    return Random.Shared.Next(min, max);
  }

  [McpServerTool]
  [Description("Returns the current UTC date and time as an ISO 8601 formatted string.")]
  public string GetTimestamp()
  {
    return DateTime.UtcNow.ToString("O");
  }

  [McpServerTool]
  [Description("Echoes back the provided message text.")]
  public string Echo(
    [Description("The message to echo back")] string message)
  {
    return message;
  }

  [McpServerTool]
  [Description("Returns a simulated list of demo users for testing purposes.")]
  public List<DemoUser> ListUsers()
  {
    var users = new List<DemoUser>
    {
      new("alice", "admin"),
      new("bob", "editor"),
      new("charlie", "viewer"),
      new("diana", "viewer"),
    };

    ToolLogMessages.LogListUsers(_logger, users.Count);

    return users;
  }

  [McpServerTool]
  [Description("Returns simulated server statistics for testing purposes.")]
  public ServerStats GetServerStats()
  {
    var stats = new ServerStats(
      Uptime: "3 days, 7 hours, 42 minutes",
      RequestCount: 15482);

    ToolLogMessages.LogServerStats(_logger, stats.Uptime, stats.RequestCount);

    return stats;
  }
}
