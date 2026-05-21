using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AuthenticatedHttpMcpServer.Infrastructure.Authentication;

internal class ApiKeyAuthHandlerOptions : AuthenticationSchemeOptions
{
    public string HeaderName { get; set; } = Constants.Auth.AzureApiKeyName;
    public Func<string, bool> ValidateKey { get; set; } = _ => false;
}

internal class ApiKeyAuthHandler(
    IOptionsMonitor<ApiKeyAuthHandlerOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<ApiKeyAuthHandlerOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Options.HeaderName, out var keyValues))
            return Task.FromResult(AuthenticateResult.NoResult());

        var key = keyValues.ToString();
        if (!Options.ValidateKey(key))
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key"));

        var identity = new ClaimsIdentity(Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
