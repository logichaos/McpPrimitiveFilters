using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace McpServer.Infrastructure;

public static class RateLimiterPolicyNames
{
  public const string McpRateLimits = "McpRateLimits";
  public const string Fixed = "Fixed";
}

public class RateLimiterOptions
{
  public const string RateLimitOptionsSectionName = "RateLimiterOptions";

  public bool Enabled { get; set; } = true;

  public FixedWindowRateLimiterOptions? FixedWindowRateLimit { get; set; }
  public FixedWindowRateLimiterOptions? McpWindowRateLimit { get; set; }
}

public static partial class ApiBuilder
{
  internal sealed class RateLimiterMarker;

  public static bool IsRateLimiterConfigured(this IServiceProvider services) =>
      services.GetService<RateLimiterMarker>() is not null;

  public static IServiceCollection ConfigureRateLimiter(this IServiceCollection services, IConfiguration configuration)
  {
    var rateLimits = configuration
      .GetRequiredSection(RateLimiterOptions.RateLimitOptionsSectionName)
      .Get<RateLimiterOptions>()!;

    services.ConfigureRateLimiter(rateLimits);
    return services;
  }

  internal static IServiceCollection ConfigureRateLimiter(this IServiceCollection services, RateLimiterOptions rateLimits)
  {
    if (!rateLimits.Enabled)
      return services;

    var fixedRateLimit = rateLimits.FixedWindowRateLimit
      ?? throw new InvalidOperationException(
        $"{RateLimiterOptions.RateLimitOptionsSectionName}:FixedWindowRateLimit is required.");
    var mcpRateLimit = rateLimits.McpWindowRateLimit
      ?? throw new InvalidOperationException(
        $"{RateLimiterOptions.RateLimitOptionsSectionName}:McpWindowRateLimit is required.");

    services.AddRateLimiter(options =>
    {
      options.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;
      options.OnRejected = CreateOnRejectedHandler();

      options.AddPolicy(RateLimiterPolicyNames.McpRateLimits, httpContext =>
        CreateFixedWindowPartition(httpContext, mcpRateLimit));
      options.AddPolicy(RateLimiterPolicyNames.Fixed, httpContext =>
        CreateFixedWindowPartition(httpContext, fixedRateLimit));
    });

    services.AddSingleton(new RateLimiterMarker());

    return services;
  }

  internal static string GetPartitionKey(HttpContext httpContext)
  {
    return httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
      ?? httpContext.Connection.RemoteIpAddress?.ToString()
      ?? "unknown";
  }

  internal static void SetRateLimitHeaders(HttpContext httpContext, FixedWindowRateLimiterOptions rateLimit)
  {
    httpContext.Response.Headers["X-Rate-Limit-Limit"] =
      rateLimit.PermitLimit.ToString();
  }

  internal static Func<OnRejectedContext, CancellationToken, ValueTask> CreateOnRejectedHandler()
  {
    return async (context, cancellationToken) =>
    {
      if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
      {
        await WriteRejectionResponse(context.HttpContext.Response, retryAfter, cancellationToken);
      }
    };
  }

  internal static async Task WriteRejectionResponse(HttpResponse response, TimeSpan retryAfter, CancellationToken cancellationToken)
  {
    var totalSeconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
    response.Headers.RetryAfter = totalSeconds.ToString();
    response.ContentType = "text/plain";

    var timeLeft = totalSeconds >= 60
      ? $"{totalSeconds / 60}m {totalSeconds % 60}s"
      : $"{totalSeconds}s";

    await response.WriteAsync(
      $"Rate limit reached. Retry after {timeLeft}.", cancellationToken);
  }

  internal static RateLimitPartition<string> CreateFixedWindowPartition(
    HttpContext httpContext, FixedWindowRateLimiterOptions rateLimit)
  {
    SetRateLimitHeaders(httpContext, rateLimit);
    return RateLimitPartition.GetFixedWindowLimiter(
      GetPartitionKey(httpContext),
      _ => rateLimit);
  }
}
