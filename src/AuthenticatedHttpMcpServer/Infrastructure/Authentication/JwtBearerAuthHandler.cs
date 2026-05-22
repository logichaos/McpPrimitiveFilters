using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace AuthenticatedHttpMcpServer.Infrastructure.Authentication;

internal class JwtBearerAuthHandlerOptions : AuthenticationSchemeOptions
{
    public IEnumerable<string>? ValidAudiences { get; set; }
    public IEnumerable<string>? ValidIssuers { get; set; }
    public bool RequireSignedTokens { get; set; } = true;
    public IConfigurationManager<OpenIdConnectConfiguration>? OidcConfigurationManager { get; set; }
}

internal class JwtBearerAuthHandler(
    IOptionsMonitor<JwtBearerAuthHandlerOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<JwtBearerAuthHandlerOptions>(options, logger, encoder)
{
    private readonly JsonWebTokenHandler _tokenHandler = new() { MapInboundClaims = true };

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return AuthenticateResult.NoResult();

        var header = authHeader.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = header["Bearer ".Length..].Trim();

        // Enforce audience validation in signed-token (production) mode.
        // TokenValidationParameters with null ValidAudiences silently skips
        // audience validation, so we reject early when not configured.
        if (Options.RequireSignedTokens
            && (Options.ValidAudiences is null || !Options.ValidAudiences.Any()))
        {
            Logger.LogError("JWT validation is misconfigured: RequireSignedTokens is true " +
                            "but no ValidAudiences have been set");
            return AuthenticateResult.Fail(
                "Server authentication is misconfigured — no valid audiences configured");
        }

        var validationParameters = new TokenValidationParameters
        {
            ValidAudiences = Options.ValidAudiences,
            ValidIssuers = Options.ValidIssuers,
            RequireSignedTokens = Options.RequireSignedTokens,
            ValidateIssuerSigningKey = Options.RequireSignedTokens,
        };

        if (!Options.RequireSignedTokens)
        {
            validationParameters.SignatureValidator = (t, _) => new JsonWebToken(t);
        }
        else if (Options.OidcConfigurationManager is { } configManager)
        {
            var oidcConfig = await configManager.GetConfigurationAsync(Context.RequestAborted);
            validationParameters.IssuerSigningKeys = oidcConfig.SigningKeys;
        }

        var result = await _tokenHandler.ValidateTokenAsync(token, validationParameters);

        if (!result.IsValid)
        {
            Logger.LogDebug(result.Exception, "JWT validation failed");
            return AuthenticateResult.Fail(result.Exception);
        }

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(result.ClaimsIdentity), Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}
