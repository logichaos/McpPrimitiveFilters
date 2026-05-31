using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using McpServer.Infrastructure;

namespace McpServer.Unit.Tests;

public class RateLimiterTests
{
    [Test]
    public async Task GetPartitionKey_ReturnsNameIdentifier_WhenClaimPresent()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "user-42")
        ]));

        var key = ApiBuilder.GetPartitionKey(context);

        await Assert.That(key).IsEqualTo("user-42");
    }

    [Test]
    public async Task GetPartitionKey_FallsBackToRemoteIp_WhenNoNameIdentifierClaim()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        context.User = new ClaimsPrincipal();

        var key = ApiBuilder.GetPartitionKey(context);

        await Assert.That(key).IsEqualTo("10.0.0.1");
    }

    [Test]
    public async Task GetPartitionKey_ReturnsUnknown_WhenNoUserAndNoIp()
    {
        var context = new DefaultHttpContext();

        var key = ApiBuilder.GetPartitionKey(context);

        await Assert.That(key).IsEqualTo("unknown");
    }

    [Test]
    public async Task GetPartitionKey_PrefersClaimOverIp()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        context.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "authenticated-user")
        ]));

        var key = ApiBuilder.GetPartitionKey(context);

        await Assert.That(key).IsEqualTo("authenticated-user");
    }

    [Test]
    public async Task SetRateLimitHeaders_SetsPermitLimitHeader()
    {
        var context = new DefaultHttpContext();
        var rateLimit = new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1)
        };

        ApiBuilder.SetRateLimitHeaders(context, rateLimit);

        await Assert.That(context.Response.Headers["X-Rate-Limit-Limit"].ToString())
            .IsEqualTo("100");
    }

    [Test]
    public async Task SetRateLimitHeaders_UsesConfiguredPermitLimit()
    {
        var context = new DefaultHttpContext();
        var rateLimit = new FixedWindowRateLimiterOptions
        {
            PermitLimit = 40,
            Window = TimeSpan.FromMinutes(2)
        };

        ApiBuilder.SetRateLimitHeaders(context, rateLimit);

        await Assert.That(context.Response.Headers["X-Rate-Limit-Limit"].ToString())
            .IsEqualTo("40");
    }

    [Test]
    public async Task SetRateLimitHeaders_ShowsMcpSpecificLimit()
    {
        var context = new DefaultHttpContext();
        var mcpRateLimit = new FixedWindowRateLimiterOptions
        {
            PermitLimit = 200,
            Window = TimeSpan.FromSeconds(30)
        };

        ApiBuilder.SetRateLimitHeaders(context, mcpRateLimit);

        await Assert.That(context.Response.Headers["X-Rate-Limit-Limit"].ToString())
            .IsEqualTo("200");
    }

    [Test]
    public async Task WriteRejectionResponse_SetsRetryAfterHeader()
    {
        var context = new DefaultHttpContext();
        var retryAfter = TimeSpan.FromSeconds(120);

        await ApiBuilder.WriteRejectionResponse(context.Response, retryAfter, CancellationToken.None);

        await Assert.That(context.Response.Headers.RetryAfter.ToString())
            .IsEqualTo("120");
    }

    [Test]
    public async Task WriteRejectionResponse_SetsContentTypePlainText()
    {
        var context = new DefaultHttpContext();
        var retryAfter = TimeSpan.FromSeconds(30);

        await ApiBuilder.WriteRejectionResponse(context.Response, retryAfter, CancellationToken.None);

        await Assert.That(context.Response.ContentType)
            .IsEqualTo("text/plain");
    }

    [Test]
    public async Task WriteRejectionResponse_WritesExpectedBody()
    {
        var context = new DefaultHttpContext();
        var bodyStream = new MemoryStream();
        context.Response.Body = bodyStream;
        var retryAfter = TimeSpan.FromSeconds(45);

        await ApiBuilder.WriteRejectionResponse(context.Response, retryAfter, CancellationToken.None);

        bodyStream.Position = 0;
        using var reader = new StreamReader(bodyStream);
        var body = await reader.ReadToEndAsync();

        await Assert.That(body)
            .Contains("Rate limit reached")
            .And.Contains("45s");
    }

    [Test]
    public async Task WriteRejectionResponse_ShowsExactSeconds()
    {
        var context = new DefaultHttpContext();
        var bodyStream = new MemoryStream();
        context.Response.Body = bodyStream;
        var remaining = TimeSpan.FromSeconds(125);

        await ApiBuilder.WriteRejectionResponse(context.Response, remaining, CancellationToken.None);

        bodyStream.Position = 0;
        using var reader = new StreamReader(bodyStream);
        var body = await reader.ReadToEndAsync();

        await Assert.That(body)
            .Contains("Rate limit reached")
            .And.Contains("125s");
    }

    [Test]
    public async Task WriteRejectionResponse_FloorsToWholeSeconds()
    {
        var context = new DefaultHttpContext();
        var remaining = TimeSpan.FromMilliseconds(59001);

        await ApiBuilder.WriteRejectionResponse(context.Response, remaining, CancellationToken.None);

        await Assert.That(context.Response.Headers.RetryAfter.ToString())
            .IsEqualTo("59");
    }

    [Test]
    public async Task CreateFixedWindowPartition_UsesUserPartitionKey()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "alice")
        ]));
        var rateLimit = new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1)
        };

        var partition = ApiBuilder.CreateFixedWindowPartition(context, rateLimit);

        await Assert.That(partition.PartitionKey).IsEqualTo("alice");
    }

    [Test]
    public async Task CreateFixedWindowPartition_SetsRateLimitHeader()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "bob")
        ]));
        var rateLimit = new FixedWindowRateLimiterOptions
        {
            PermitLimit = 25,
            Window = TimeSpan.FromMinutes(3)
        };

        ApiBuilder.CreateFixedWindowPartition(context, rateLimit);

        await Assert.That(context.Response.Headers["X-Rate-Limit-Limit"].ToString())
            .IsEqualTo("25");
    }

    [Test]
    public async Task CreateFixedWindowPartition_UsesIpAsFallbackKey()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.2");
        var mcpLimit = new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromSeconds(30)
        };

        var partition = ApiBuilder.CreateFixedWindowPartition(context, mcpLimit);

        await Assert.That(partition.PartitionKey).IsEqualTo("10.0.0.2");
    }

    [Test]
    public async Task ConfigureRateLimiter_RegistersBothPolicies()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiterOptions:FixedWindowRateLimit:PermitLimit"] = "50",
                ["RateLimiterOptions:FixedWindowRateLimit:Window"] = "00:01:00",
                ["RateLimiterOptions:McpWindowRateLimit:PermitLimit"] = "200",
                ["RateLimiterOptions:McpWindowRateLimit:Window"] = "00:00:30",
            })
            .Build();

        services.ConfigureRateLimiter(config);
        var provider = services.BuildServiceProvider();

        await Assert.That(provider).IsNotNull();
    }

    [Test]
    public async Task ConfigureRateLimiter_SetsTooManyRequestsStatusCode()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiterOptions:FixedWindowRateLimit:PermitLimit"] = "1",
                ["RateLimiterOptions:FixedWindowRateLimit:Window"] = "00:01:00",
                ["RateLimiterOptions:McpWindowRateLimit:PermitLimit"] = "5",
                ["RateLimiterOptions:McpWindowRateLimit:Window"] = "00:01:00",
            })
            .Build();

        services.ConfigureRateLimiter(config);
        var provider = services.BuildServiceProvider();

        var rateLimiterOpts = provider
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.RateLimiting.RateLimiterOptions>>()
            .Value;

        await Assert.That(rateLimiterOpts.RejectionStatusCode)
            .IsEqualTo((int)HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task PolicyNames_McpRateLimits_MatchesLiteral()
    {
        await Assert.That(RateLimiterPolicyNames.McpRateLimits)
            .IsEqualTo("McpRateLimits");
    }

    [Test]
    public async Task PolicyNames_Fixed_MatchesLiteral()
    {
        await Assert.That(RateLimiterPolicyNames.Fixed)
            .IsEqualTo("Fixed");
    }

    [Test]
    public async Task RateLimiterOptions_HasSeparateMcpAndFixedLimits()
    {
        var options = new RateLimiterOptions
        {
            FixedWindowRateLimit = new FixedWindowRateLimiterOptions
            {
                PermitLimit = 40,
                Window = TimeSpan.FromMinutes(2)
            },
            McpWindowRateLimit = new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromSeconds(30)
            }
        };

        await Assert.That(options.FixedWindowRateLimit.PermitLimit).IsEqualTo(40);
        await Assert.That(options.McpWindowRateLimit.PermitLimit).IsEqualTo(100);
        await Assert.That(options.FixedWindowRateLimit.Window).IsEqualTo(TimeSpan.FromMinutes(2));
        await Assert.That(options.McpWindowRateLimit.Window).IsEqualTo(TimeSpan.FromSeconds(30));
    }
}
