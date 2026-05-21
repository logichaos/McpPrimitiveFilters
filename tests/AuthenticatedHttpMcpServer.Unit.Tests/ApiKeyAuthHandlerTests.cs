using System.Text.Encodings.Web;
using AuthenticatedHttpMcpServer.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AuthenticatedHttpMcpServer.Unit.Tests;

public class ApiKeyAuthHandlerTests
{
    private const string DefaultHeader = "Ocp-Apim-Subscription-Key";
    private const string SchemeName = "ApiKey-Header";

    private static async Task<(ApiKeyAuthHandler handler, DefaultHttpContext context)> CreateHandler(
        Func<string, bool> validateKey,
        string? headerName = null)
    {
        var opts = new ApiKeyAuthHandlerOptions { ValidateKey = validateKey };
        if (headerName is not null)
            opts.HeaderName = headerName;

        var monitor = new StubOptionsMonitor<ApiKeyAuthHandlerOptions>(opts);
        var handler = new ApiKeyAuthHandler(monitor, NullLoggerFactory.Instance, UrlEncoder.Default);
        var scheme = new AuthenticationScheme(SchemeName, SchemeName, typeof(ApiKeyAuthHandler));
        var context = new DefaultHttpContext();
        await handler.InitializeAsync(scheme, context);
        return (handler, context);
    }

    [Test]
    public async Task MissingHeader_ReturnsNoResult()
    {
        var (handler, _) = await CreateHandler(_ => true);

        var result = await handler.AuthenticateAsync();

        await Assert.That(result.None).IsTrue();
    }

    [Test]
    public async Task InvalidKey_ReturnsFail()
    {
        var (handler, context) = await CreateHandler(k => k == "valid");
        context.Request.Headers[DefaultHeader] = "wrong";

        var result = await handler.AuthenticateAsync();

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.Failure!.Message).IsEqualTo("Invalid API key");
    }

    [Test]
    public async Task ValidKey_ReturnsSuccess()
    {
        var (handler, context) = await CreateHandler(k => k == "valid");
        context.Request.Headers[DefaultHeader] = "valid";

        var result = await handler.AuthenticateAsync();

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task ValidKey_TicketHasCorrectScheme()
    {
        var (handler, context) = await CreateHandler(k => k == "valid");
        context.Request.Headers[DefaultHeader] = "valid";

        var result = await handler.AuthenticateAsync();

        await Assert.That(result.Ticket!.AuthenticationScheme).IsEqualTo(SchemeName);
    }

    [Test]
    public async Task CustomHeaderName_UsesConfiguredHeader()
    {
        var (handler, context) = await CreateHandler(k => k == "valid", headerName: "X-Custom-Key");
        context.Request.Headers["X-Custom-Key"] = "valid";

        var result = await handler.AuthenticateAsync();

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task CustomHeaderName_DefaultHeaderIgnored()
    {
        var (handler, context) = await CreateHandler(k => k == "valid", headerName: "X-Custom-Key");
        context.Request.Headers[DefaultHeader] = "valid";

        var result = await handler.AuthenticateAsync();

        await Assert.That(result.None).IsTrue();
    }

    private sealed class StubOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
