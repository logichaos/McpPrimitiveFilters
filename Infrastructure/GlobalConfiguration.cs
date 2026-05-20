using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Caching.Hybrid;

namespace AuthenticatedHttpMcpServer.Infrastructure;

internal static class GlobalConfigurations
{
  public static SettingsModel? ApiSettings { get; set; }
}

internal class SettingsModel
{
  public required TokenValidationParameters TokenValidation { get; set; }
  public required FixedWindowRateLimiterOptions FixedWindowRateLimit { get; set; }
  public required HybridCacheEntryOptions Cache { get; set; }
  public ToolExposureSettings? ToolExposure { get; set; }
}

internal class ToolExposureSettings
{
  public IList<string>? BlockedTools { get; set; }
}