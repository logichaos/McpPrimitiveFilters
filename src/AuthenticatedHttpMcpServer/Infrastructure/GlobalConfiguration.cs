using System.Threading.RateLimiting;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.IdentityModel.Tokens;

namespace AuthenticatedHttpMcpServer.Infrastructure;

internal static class GlobalConfigurations
{
  public static SettingsModel? ApiSettings { get; set; }
}

internal class SettingsModel
{
  public required TokenValidationParameters TokenValidation { get; init; }
  public required FixedWindowRateLimiterOptions FixedWindowRateLimit { get; init; }
  public required HybridCacheEntryOptions Cache { get; init; }
  public ToolExposureSettings? ToolExposure { get; init; }
  public EntraIdOptions? EntraId { get; set; }
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