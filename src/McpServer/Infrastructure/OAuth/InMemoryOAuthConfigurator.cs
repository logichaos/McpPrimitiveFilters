using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace McpServer.Infrastructure.OAuth;

public sealed class InMemoryOAuthConfigurator : IOAuthSchemeConfigurator
{
    public const string ProviderTypeName = "InMemory";
    public string ProviderType => "InMemory";

    public void Configure(JwtBearerOptions options, OAuthSchemeConfig scheme, OAuthOptions oauth)
    {
        if (string.IsNullOrEmpty(scheme.AuthorityUrl))
            throw new InvalidOperationException(
                $"OAuth scheme '{nameof(InMemoryOAuthConfigurator)}' requires a non-empty AuthorityUrl.");

        var authority = scheme.AuthorityUrl;
        var audience = scheme.Audience ?? oauth.ServerUrl;
        var issuer = scheme.Issuer ?? authority;

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
            ValidIssuer = issuer,
            NameClaimType = "name",
            RoleClaimType = "roles"
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var name = context.Principal?.Identity?.Name ?? "unknown";
                var email = context.Principal?.FindFirstValue("preferred_username") ?? "unknown";
                Console.WriteLine($"[InMemory] Token validated for: {name} ({email})");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"[InMemory] Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            }
        };
    }
}
