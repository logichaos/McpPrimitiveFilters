namespace McpServer.Infrastructure;

public sealed class McpOptions
{
  public const string SectionName = "MCP";

  public string Transport { get; set; } = "http";
}

public sealed class McpCoreOptions
{
  public const string SectionName = "Mcp";

  public string[] AllowedOrigins { get; set; } = [];
}
