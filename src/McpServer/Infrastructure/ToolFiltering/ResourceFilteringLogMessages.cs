namespace McpServer.Infrastructure.ToolFiltering;

internal static partial class ResourceFilteringLogMessages
{
    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Resource filtering: user not authenticated — allowing all {Count} resources")]
    public static partial void NotAuthenticated(
        ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Resource filtering: user={User}, scopes={Scopes}")]
    public static partial void Scopes(
        ILogger logger, string? user, IReadOnlyList<string> scopes);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Resource filtering: user={User} has mcp.resources.all scope — allowing all resources")]
    public static partial void AllAccess(
        ILogger logger, string? user);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Resource filtering: user={User} allowed resource '{Resource}' via scope '{Scope}'")]
    public static partial void Allowed(
        ILogger logger, string? user, string resource, string scope);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Resource filtering: user={User} denied resource '{Resource}' — missing scope '{Scope}'")]
    public static partial void Denied(
        ILogger logger, string? user, string resource, string scope);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Resource filtering result: {Allowed} allowed, {Denied} denied")]
    public static partial void Result(
        ILogger logger, int allowed, int denied);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Resource read denied: user={User}, resource={Resource}, uri={Uri}")]
    public static partial void ReadDenied(
        ILogger logger, string? user, string resource, string? uri);
}
