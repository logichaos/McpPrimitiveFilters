using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace McpServer.Infrastructure.OAuth;

public sealed class EntraIdOAuthConfigurator : IOAuthSchemeConfigurator
{
    public const string ProviderTypeName = "EntraId";
    public string ProviderType => "EntraId";

    public void Configure(JwtBearerOptions options, OAuthSchemeConfig scheme, OAuthOptions oauth)
    {
        if (string.IsNullOrEmpty(scheme.TenantId))
            throw new InvalidOperationException(
                $"OAuth scheme '{nameof(EntraIdOAuthConfigurator)}' requires a non-empty TenantId.");

        var instance = scheme.Instance ?? "https://login.microsoftonline.com/";
        var authority = $"{instance.TrimEnd('/')}/{scheme.TenantId}/v2.0";
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

        // Don't set ValidIssuer explicitly — the JWT bearer middleware auto-discovers
        // the valid issuer from the OIDC metadata at {authority}/.well-known/openid-configuration.
        // Entra ID v2.0 tokens may carry an iss that differs from the authority URL
        // (e.g. https://sts.windows.net/{tenantId}/), so hardcoding it causes rejections.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidAudience = audience,
            NameClaimType = "name",
            RoleClaimType = "roles"
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var name = context.Principal?.Identity?.Name ?? "unknown";
                var oid = context.Principal?.FindFirstValue("oid") ?? "unknown";
                Console.WriteLine($"[EntraId] Token validated for: {name} (oid: {oid})");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"[EntraId] Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            }
        };
    }
}
