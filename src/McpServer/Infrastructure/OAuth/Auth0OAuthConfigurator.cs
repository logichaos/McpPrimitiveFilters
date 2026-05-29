using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace McpServer.Infrastructure.OAuth;

public sealed class Auth0OAuthConfigurator : IOAuthSchemeConfigurator
{
    public const string ProviderTypeName = "Auth0";
    public string ProviderType => "Auth0";

    public void Configure(JwtBearerOptions options, OAuthSchemeConfig scheme, OAuthOptions oauth)
    {
        if (string.IsNullOrEmpty(scheme.Domain))
            throw new InvalidOperationException(
                $"OAuth scheme '{nameof(Auth0OAuthConfigurator)}' requires a non-empty Domain.");

        var authority = $"https://{scheme.Domain.TrimEnd('/')}/";
        var audience = scheme.Audience
            ?? scheme.ClientId
            ?? oauth.ServerUrl;

        options.Authority = authority;

        if (scheme.DisableBackchannelSslValidation)
        {
            options.BackchannelHttpHandler = new SocketsHttpHandler
            {
                SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = (_, _, _, _) => true
                }
            };
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = audience,
            NameClaimType = "name",
            // Auth0 uses custom claim URIs rather than the standard "roles"
            RoleClaimType = "https://schemas.example.com/roles"
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var name = context.Principal?.Identity?.Name ?? "unknown";
                var sub = context.Principal?.FindFirstValue("sub") ?? "unknown";
                Console.WriteLine($"[Auth0] Token validated for: {name} (sub: {sub})");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"[Auth0] Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            }
        };
    }
}
