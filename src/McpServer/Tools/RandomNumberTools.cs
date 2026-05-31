using System.ComponentModel;
using ModelContextProtocol.Server;

namespace McpServer.Tools;

public record DemoUser(string Username, string Role);

public record ServerStats(string Uptime, int RequestCount);

public class RandomNumberTools
{
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
    return new List<DemoUser>
    {
      new("alice", "admin"),
      new("bob", "editor"),
      new("charlie", "viewer"),
      new("diana", "viewer"),
    };
  }

  [McpServerTool]
  [Description("Returns simulated server statistics for testing purposes.")]
  public ServerStats GetServerStats()
  {
    return new ServerStats(
      Uptime: "3 days, 7 hours, 42 minutes",
      RequestCount: 15482);
  }
}
