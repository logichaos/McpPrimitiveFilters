using System.Net;
using System.Threading.RateLimiting;
using AuthenticatedHttpMcpServer.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AuthenticatedHttpMcpServer.Unit.Tests;

public class AddRateLimitServicesTests
{
    private static RateLimiterOptions BuildOptions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRateLimitServices();
        return services.BuildServiceProvider()
            .GetRequiredService<IOptions<RateLimiterOptions>>().Value;
    }

    private static async Task InvokeOnRejected(
        RateLimiterOptions opts, DefaultHttpContext ctx, RateLimitLease lease)
    {
        await opts.OnRejected!(
            new OnRejectedContext { HttpContext = ctx, Lease = lease },
            CancellationToken.None);
    }

    [Test]
    public async Task RejectionStatusCode_Is429()
    {
        var opts = BuildOptions();

        await Assert.That(opts.RejectionStatusCode)
            .IsEqualTo((int)HttpStatusCode.TooManyRequests);
    }

    [Test]
    public async Task OnRejected_WithRetryAfterMetadata_SetsRetryAfterHeader()
    {
        var opts = BuildOptions();
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();

        await InvokeOnRejected(opts, ctx, new RetryAfterLease(TimeSpan.FromSeconds(42)));

        await Assert.That(ctx.Response.Headers.RetryAfter.ToString()).IsEqualTo("42");
    }

    [Test]
    public async Task OnRejected_WithRetryAfterMetadata_SetsContentTypePlainText()
    {
        var opts = BuildOptions();
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();

        await InvokeOnRejected(opts, ctx, new RetryAfterLease(TimeSpan.FromSeconds(30)));

        await Assert.That(ctx.Response.ContentType).IsEqualTo("text/plain");
    }

    [Test]
    public async Task OnRejected_WithRetryAfterMetadata_WritesBodyWithRetryAfterSeconds()
    {
        var opts = BuildOptions();
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();

        await InvokeOnRejected(opts, ctx, new RetryAfterLease(TimeSpan.FromSeconds(60)));

        ctx.Response.Body.Position = 0;
        var body = await new StreamReader(ctx.Response.Body).ReadToEndAsync();
        await Assert.That(body).Contains("60");
    }

    [Test]
    public async Task OnRejected_WithoutRetryAfterMetadata_DoesNotSetRetryAfterHeader()
    {
        var opts = BuildOptions();
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();

        await InvokeOnRejected(opts, ctx, new NoMetadataLease());

        await Assert.That(ctx.Response.Headers.RetryAfter.Count).IsEqualTo(0);
    }

    private sealed class RetryAfterLease(TimeSpan retryAfter) : RateLimitLease
    {
        public override bool IsAcquired => false;
        public override IEnumerable<string> MetadataNames => [MetadataName.RetryAfter.Name];

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            if (metadataName == MetadataName.RetryAfter.Name)
            {
                metadata = retryAfter;
                return true;
            }
            metadata = null;
            return false;
        }
    }

    private sealed class NoMetadataLease : RateLimitLease
    {
        public override bool IsAcquired => false;
        public override IEnumerable<string> MetadataNames => [];

        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            metadata = null;
            return false;
        }
    }
}
