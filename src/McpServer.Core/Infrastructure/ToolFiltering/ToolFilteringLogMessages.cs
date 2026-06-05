namespace McpServer.Infrastructure.ToolFiltering;

internal static partial class ToolFilteringLogMessages
{
    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Tool filtering: user not authenticated — allowing all {Count} tools")]
    public static partial void NotAuthenticated(
        ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Tool filtering: user={User}, scopes={Scopes}")]
    public static partial void Scopes(
        ILogger logger, string? user, IReadOnlyList<string> scopes);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Tool filtering: user={User} has mcp.tools.all scope — allowing all tools")]
    public static partial void AllAccess(
        ILogger logger, string? user);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Tool filtering: user={User} allowed tool '{Tool}' via scope '{Scope}'")]
    public static partial void Allowed(
        ILogger logger, string? user, string tool, string scope);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Tool filtering: user={User} denied tool '{Tool}' — missing scope '{Scope}'")]
    public static partial void Denied(
        ILogger logger, string? user, string tool, string scope);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Tool filtering result: {Allowed} allowed, {Denied} denied")]
    public static partial void Result(
        ILogger logger, int allowed, int denied);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Tool call denied: user={User}, tool={Tool}")]
    public static partial void CallDenied(
        ILogger logger, string? user, string tool);
}
