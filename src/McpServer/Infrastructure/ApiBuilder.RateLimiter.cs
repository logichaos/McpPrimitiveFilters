using System.Collections.Concurrent;
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

  /// <summary>
  /// Tracks the time of the first rate-limit rejection per partition key
  /// so we can calculate the actual remaining time in the current window.
  /// .NET's FixedWindowRateLimiter always returns the full window duration
  /// as RetryAfter (https://github.com/dotnet/runtime/issues/92557).
  /// </summary>
  private static readonly ConcurrentDictionary<string, DateTimeOffset> s_firstRejections = new();

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

    // Register rate limit options so the rejection handler can access window durations
    services.AddSingleton(rateLimits);

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
      // Determine which policy was hit and get its window duration
      var rateLimits = context.HttpContext.RequestServices.GetRequiredService<RateLimiterOptions>();
      var window = context.HttpContext.GetEndpoint()?.Metadata
          .GetMetadata<EnableRateLimitingAttribute>()?.PolicyName switch
      {
        RateLimiterPolicyNames.McpRateLimits => rateLimits.McpWindowRateLimit?.Window,
        RateLimiterPolicyNames.Fixed => rateLimits.FixedWindowRateLimit?.Window,
        _ => null
      } ?? TimeSpan.FromMinutes(1);

      var partitionKey = GetPartitionKey(context.HttpContext);
      var now = DateTimeOffset.UtcNow;

      // Track the first rejection in this window so we can show a
      // decreasing countdown instead of the static full-window value
      // that .NET's FixedWindowRateLimiter incorrectly reports.
      var firstReject = s_firstRejections.GetOrAdd(partitionKey, now);
      var elapsed = now - firstReject;

      if (elapsed > window)
      {
        // We're in a new window — reset the tracker
        firstReject = now;
        s_firstRejections[partitionKey] = now;
        elapsed = TimeSpan.Zero;
      }

      var remaining = window - elapsed;
      if (remaining < TimeSpan.Zero)
        remaining = TimeSpan.Zero;

      await WriteRejectionResponse(context.HttpContext.Response, remaining, cancellationToken);
    };
  }

  internal static async Task WriteRejectionResponse(HttpResponse response, TimeSpan remaining, CancellationToken cancellationToken)
  {
    var totalSeconds = (int)Math.Floor(remaining.TotalSeconds);
    response.Headers.RetryAfter = totalSeconds.ToString();
    response.ContentType = "text/plain";

    await response.WriteAsync(
      $"Rate limit reached. Retry after {totalSeconds}s.", cancellationToken);
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
