using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using AuthenticatedHttpMcpServer.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace AuthenticatedHttpMcpServer.Unit.Tests;

public class JwtBearerAuthHandlerTests
{
    private const string SchemeName = "Bearer";
    private const string DefaultAudience = "test-api";
    private const string DefaultIssuer = "https://test.example.com";
    private static readonly SymmetricSecurityKey TestKey =
        new(Encoding.UTF8.GetBytes("test-secret-key-that-is-long-enough-for-hmac-sha256"));

    private static string CreateToken(
        string? audience = DefaultAudience,
        string? issuer = DefaultIssuer,
        DateTime? expires = null,
        IEnumerable<Claim>? claims = null)
    {
        var handler = new JsonWebTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims ?? [new Claim("sub", "user1")]),
            Audience = audience,
            Issuer = issuer,
            Expires = expires ?? DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(TestKey, SecurityAlgorithms.HmacSha256),
        };
        return handler.CreateToken(descriptor);
    }

    private static OpenIdConnectConfiguration OidcConfigWithTestKey()
    {
        var config = new OpenIdConnectConfiguration();
        config.SigningKeys.Add(TestKey);
        return config;
    }

    private static async Task<(JwtBearerAuthHandler handler, DefaultHttpContext context)> CreateHandler(
        Action<JwtBearerAuthHandlerOptions>? configure = null)
    {
        var opts = new JwtBearerAuthHandlerOptions
        {
            ValidAudiences = [DefaultAudience],
            ValidIssuers = [DefaultIssuer],
            RequireSignedTokens = true,
            OidcConfigurationManager = new StubConfigurationManager(OidcConfigWithTestKey()),
        };
        configure?.Invoke(opts);

        var monitor = new StubOptionsMonitor<JwtBearerAuthHandlerOptions>(opts);
        var handler = new JwtBearerAuthHandler(monitor, NullLoggerFactory.Instance, UrlEncoder.Default);
        var scheme = new AuthenticationScheme(SchemeName, SchemeName, typeof(JwtBearerAuthHandler));
        var context = new DefaultHttpContext();
        await handler.InitializeAsync(scheme, context);
        return (handler, context);
    }

    [Test]
    public async Task MissingAuthorizationHeader_ReturnsNoResult()
    {
        var (handler, _) = await CreateHandler();

        var result = await handler.AuthenticateAsync();

        await Assert.That(result.None).IsTrue();
    }

    [Test]
    public async Task NonBearerScheme_ReturnsNoResult()
    {
        var (handler, context) = await CreateHandler();
        context.Request.Headers.Authorization = "Basic dXNlcjpwYXNz";

        var result = await handler.AuthenticateAsync();

        await Assert.That(result.None).IsTrue();
    }

    [Test]
    public async Task MalformedToken_ReturnsFail()
    {
        var (handler, context) = await CreateHandler();
        context.Request.Headers.Authorization = "Bearer not-a-jwt";

        var result = await handler.AuthenticateAsync();

        await Assert.That(result.Succeeded).IsFalse();
    }

    [Test]
    public async Task ValidToken_ReturnsSuccess()
    {
        var (handler, context) = await CreateHandler();
        context.Request.Headers.Authorization = $"Bearer {CreateToken()}";

        var result = await handler.AuthenticateAsync();

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task ExpiredToken_ReturnsFail()
    {
        var (handler, context) = await CreateHandler();
        context.Request.Headers.Authorization =
            $"Bearer {CreateToken(expires: DateTime.UtcNow.AddHours(-1))}";

        var result = await handler.AuthenticateAsync();

        await Assert.That(result.Succeeded).IsFalse();
    }

    [Test]
    public async Task ValidAudience_ReturnsSuccess()
    {
        var (handler, context) = await CreateHandler(o => o.ValidAudiences = ["my-api"]);
        context.Request.Headers.Authorization = $"Bearer {CreateToken(audience: "my-api")}";

        var result = await handler.AuthenticateAsync();

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task WrongAudience_ReturnsFail()
    {
        var (handler, context) = await CreateHandler(o => o.ValidAudiences = ["my-api"]);
        context.Request.Headers.Authorization = $"Bearer {CreateToken(audience: "other-api")}";

        var result = await handler.AuthenticateAsync();

        await Assert.That(result.Succeeded).IsFalse();
    }

    [Test]
    public async Task ValidIssuer_ReturnsSuccess()
    {
        var (handler, context) = await CreateHandler(o => o.ValidIssuers = ["https://auth.example.com"]);
        context.Request.Headers.Authorization =
            $"Bearer {CreateToken(issuer: "https://auth.example.com")}";

        var result = await handler.AuthenticateAsync();

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task WrongIssuer_ReturnsFail()
    {
        var (handler, context) = await CreateHandler(o => o.ValidIssuers = ["https://auth.example.com"]);
        context.Request.Headers.Authorization =
            $"Bearer {CreateToken(issuer: "https://evil.example.com")}";

        var result = await handler.AuthenticateAsync();

        await Assert.That(result.Succeeded).IsFalse();
    }

    [Test]
    public async Task SignedToken_WrongKey_ReturnsFail()
    {
        var wrongKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("wrong-key-that-is-different-from-test-key-!!!!"));
        var oidcConfig = new OpenIdConnectConfiguration();
        oidcConfig.SigningKeys.Add(wrongKey);

        var (handler, context) = await CreateHandler(o =>
            o.OidcConfigurationManager = new StubConfigurationManager(oidcConfig));
        context.Request.Headers.Authorization = $"Bearer {CreateToken()}";

        var result = await handler.AuthenticateAsync();

        await Assert.That(result.Succeeded).IsFalse();
    }

    [Test]
    public async Task RequireSignedTokensFalse_AcceptsTokenWithoutKeyConfigured()
    {
        var (handler, context) = await CreateHandler(o =>
        {
            o.RequireSignedTokens = false;
            o.OidcConfigurationManager = null;
        });
        context.Request.Headers.Authorization = $"Bearer {CreateToken()}";

        var result = await handler.AuthenticateAsync();

        await Assert.That(result.Succeeded).IsTrue();
    }

    [Test]
    public async Task ValidToken_TicketHasCorrectScheme()
    {
        var (handler, context) = await CreateHandler();
        context.Request.Headers.Authorization = $"Bearer {CreateToken()}";

        var result = await handler.AuthenticateAsync();

        await Assert.That(result.Ticket!.AuthenticationScheme).IsEqualTo(SchemeName);
    }

    [Test]
    public async Task ValidToken_ClaimsPopulatedFromToken()
    {
        var (handler, context) = await CreateHandler();
        var claims = new[] { new Claim("user_id", "user42") };
        context.Request.Headers.Authorization = $"Bearer {CreateToken(claims: claims)}";

        var result = await handler.AuthenticateAsync();

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(result.Ticket!.Principal.FindFirst("user_id")?.Value).IsEqualTo("user42");
    }

    private sealed class StubOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue => value;
        public T Get(string? name) => value;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class StubConfigurationManager(OpenIdConnectConfiguration config)
        : IConfigurationManager<OpenIdConnectConfiguration>
    {
        public Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel) =>
            Task.FromResult(config);

        public void RequestRefresh() { }
    }
}
