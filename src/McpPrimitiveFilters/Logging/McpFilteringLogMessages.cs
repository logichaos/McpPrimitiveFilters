namespace McpPrimitiveFilters.Logging;

internal static partial class McpFilteringLogMessages
{
    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Mcp filtering: user not authenticated — allowing all {Count} {Type}")]
    public static partial void NotAuthenticated(ILogger logger, McpPrimitiveType type, int count);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Mcp filtering: user={User}, scopes={Scopes}, type={Type}")]
    public static partial void Scopes(ILogger logger, McpPrimitiveType type, string? user, IReadOnlyList<string> scopes);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Mcp filtering: user={User} has wildcard scope — allowing all {Type}")]
    public static partial void AllAccess(ILogger logger, McpPrimitiveType type, string? user);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Mcp filtering: user={User} allowed {Type} '{Name}'")]
    public static partial void Allowed(ILogger logger, McpPrimitiveType type, string? user, string name);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Mcp filtering: user={User} denied {Type} '{Name}'")]
    public static partial void Denied(ILogger logger, McpPrimitiveType type, string? user, string name);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Mcp filtering result: {Allowed} allowed, {Denied} denied ({Type})")]
    public static partial void Result(ILogger logger, McpPrimitiveType type, int allowed, int denied);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Mcp {Type} call denied: user={User}, name={Name}")]
    public static partial void CallDenied(ILogger logger, McpPrimitiveType type, string? user, string name);
}
