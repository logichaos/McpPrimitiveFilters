using System.Threading.RateLimiting;
using AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;
using Microsoft.Extensions.Caching.Hybrid;

namespace AuthenticatedHttpMcpServer.Infrastructure;

internal static class GlobalConfigurations
{
  public static SettingsModel? ApiSettings { get; set; }
}

internal class SettingsModel
{
  public required FixedWindowRateLimiterOptions FixedWindowRateLimit { get; init; }
  public required HybridCacheEntryOptions Cache { get; init; }
  public ToolExposureSettings? ToolExposure { get; init; }
  public string? ApiKey { get; init; }
  public EntraIdOptions? EntraId { get; set; }
  public ToolsSelectionOptions? ToolsSelection { get; set; }
  public OAuthOptions? OAuth { get; set; }
}

internal class OAuthOptions
{
  public string? Authority { get; set; }
  public string? ClientId { get; set; }
  public string[]? ValidAudiences { get; set; }
  public string[]? ValidIssuers { get; set; }
}

internal class ToolExposureSettings
{
  public IList<string>? BlockedTools { get; set; }
}

internal class EntraIdOptions
{
  public string? TenantId { get; set; }
  public string? ClientId { get; set; }
  public string? ClientSecret { get; set; }
}