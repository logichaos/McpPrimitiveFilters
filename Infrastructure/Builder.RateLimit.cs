using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace AuthenticatedHttpMcpServer.Infrastructure;

public static partial class ApiBuilder
{
  public static IServiceCollection AddRateLimitServices(this IServiceCollection services)
  {
    services.AddRateLimiter(options =>
    {
      options.RejectionStatusCode = (int)HttpStatusCode.TooManyRequests;
      options.AddPolicy("fixed", httpContext =>
      {
        httpContext.Response.Headers["X-Rate-Limit-Limit"] = GlobalConfigurations.ApiSettings!.FixedWindowRateLimit.PermitLimit.ToString();

        return RateLimitPartition.GetFixedWindowLimiter(
          partitionKey:
            httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContext.Request.Headers[Constants.Auth.AzureApiKeyName].ToString(),
            factory:
              _ => GlobalConfigurations.ApiSettings.FixedWindowRateLimit
        );
      }).OnRejected = async (context, cancellationToken) =>
      {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
          context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
          context.HttpContext.Response.ContentType = "text/plain";
          await context.HttpContext.Response.WriteAsync($"Rate limit reached. Please try again after {retryAfter.TotalSeconds} seconds.", cancellationToken: cancellationToken);
        }
      };
    });

    return services;
  }


}