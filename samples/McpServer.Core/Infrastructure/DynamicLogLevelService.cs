using ModelContextProtocol.Protocol;

namespace McpServer.Infrastructure;

public class DynamicLogLevelService
{
  public LogLevel MinLevel { get; set; } = LogLevel.Information;

  public static LogLevel MapMCPLevelToNetLevel(LoggingLevel mcpLevel) => mcpLevel switch
  {
    LoggingLevel.Debug => LogLevel.Debug,
    LoggingLevel.Info => LogLevel.Information,
    LoggingLevel.Notice => LogLevel.Information,
    LoggingLevel.Warning => LogLevel.Warning,
    LoggingLevel.Error => LogLevel.Error,
    LoggingLevel.Critical => LogLevel.Critical,
    LoggingLevel.Alert => LogLevel.Critical,
    LoggingLevel.Emergency => LogLevel.Critical,
    _ => LogLevel.Information
  };
}