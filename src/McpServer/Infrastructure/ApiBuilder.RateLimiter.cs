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
  /// Dictionary key for resolving the first-rejection tracker from DI.
  /// </summary>
  internal const string FirstRejectionsKey = "FirstRejections";

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

    services.AddSingleton(rateLimits);

    // Register the first-rejection tracker as a singleton so each
    // WebApplicationFactory instance (and integration test) has its own state.
    services.AddKeyedSingleton<ConcurrentDictionary<string, DateTimeOffset>>(FirstRejectionsKey);

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
      var rateLimits = context.HttpContext.RequestServices.GetRequiredService<RateLimiterOptions>();
      var window = context.HttpContext.GetEndpoint()?.Metadata
          .GetMetadata<EnableRateLimitingAttribute>()?.PolicyName switch
      {
        RateLimiterPolicyNames.McpRateLimits => rateLimits.McpWindowRateLimit?.Window,
        RateLimiterPolicyNames.Fixed => rateLimits.FixedWindowRateLimit?.Window,
        _ => null
      } ?? TimeSpan.FromMinutes(1);

      var firstRejections = context.HttpContext.RequestServices.GetRequiredKeyedService<ConcurrentDictionary<string, DateTimeOffset>>(FirstRejectionsKey);

      var partitionKey = GetPartitionKey(context.HttpContext);
      var now = DateTimeOffset.UtcNow;

      // Track the first rejection in this window so we can show a
      // decreasing countdown instead of the static full-window value
      // that .NET's FixedWindowRateLimiter incorrectly reports.
      var firstReject = firstRejections.GetOrAdd(partitionKey, now);
      var elapsed = now - firstReject;

      if (elapsed > window)
      {
        firstReject = now;
        firstRejections[partitionKey] = now;
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
