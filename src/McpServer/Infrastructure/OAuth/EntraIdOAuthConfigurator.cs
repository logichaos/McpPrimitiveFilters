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
        var tenantId = scheme.TenantId
            ?? throw new InvalidOperationException(
                $"OAuth scheme '{nameof(EntraIdOAuthConfigurator)}' requires TenantId.");

        var instance = scheme.Instance ?? "https://login.microsoftonline.com/";
        var authority = $"{instance.TrimEnd('/')}/{tenantId}/v2.0";
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
            ValidIssuer = authority,
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
