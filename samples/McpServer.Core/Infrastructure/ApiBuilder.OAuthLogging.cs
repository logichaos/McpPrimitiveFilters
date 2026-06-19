namespace McpServer.Infrastructure;

public static partial class ApiBuilder
{
  [LoggerMessage(Level = LogLevel.Debug,
      Message = "OAuth section '{Section}' not found — auth disabled")]
  internal static partial void LogOAuthSectionNotFound(
      ILogger logger, string section);

  [LoggerMessage(Level = LogLevel.Warning,
      Message = "OAuth section exists but has no schemes — auth disabled")]
  internal static partial void LogOAuthNoSchemes(ILogger logger);

  [LoggerMessage(Level = LogLevel.Warning,
      Message = "No OAuth schemes are enabled — auth disabled")]
  internal static partial void LogOAuthNoEnabledSchemes(ILogger logger);

  [LoggerMessage(Level = LogLevel.Information,
      Message = "OAuth enabled with {SchemeCount} scheme(s): {Schemes}")]
  internal static partial void LogOAuthEnabled(
      ILogger logger, int schemeCount, string schemes);

  [LoggerMessage(Level = LogLevel.Debug,
      Message = "OAuth config: DefaultScheme={DefaultScheme}, ServerUrl={ServerUrl}, ScopesSupported={Scopes}")]
  internal static partial void LogOAuthConfig(
      ILogger logger, string? defaultScheme, string? serverUrl, string scopes);

  [LoggerMessage(Level = LogLevel.Information,
      Message = "Multiple schemes enabled — using MultiScheme policy scheme")]
  internal static partial void LogOAuthMultiScheme(ILogger logger);

  [LoggerMessage(Level = LogLevel.Debug,
      Message = "Scheme '{Scheme}': resolved authority = {Authority}")]
  internal static partial void LogOAuthSchemeAuthority(
      ILogger logger, string scheme, string? authority);

  [LoggerMessage(Level = LogLevel.Debug,
      Message = "Scheme '{Scheme}': no authority resolved, using auto-discovery")]
  internal static partial void LogOAuthSchemeNoAuthority(
      ILogger logger, string scheme);

  [LoggerMessage(Level = LogLevel.Information,
      Message = "OAuth setup complete: {AuthServerCount} authorization server(s), CORS origins={Origins}")]
  internal static partial void LogOAuthSetupComplete(
      ILogger logger, int authServerCount, string origins);
}