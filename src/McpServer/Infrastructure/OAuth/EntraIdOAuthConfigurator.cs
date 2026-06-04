using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using System.Security.Claims;

namespace McpServer.Infrastructure.OAuth;

public sealed class EntraIdOAuthConfigurator : OAuthSchemeConfigurator
{
    public const string ProviderTypeName = "EntraId";
    public string ProviderType => "EntraId";

    public void Configure(JwtBearerOptions options, OAuthSchemeConfig scheme, OAuthOptions oauth, ILoggerFactory loggerFactory)
    {
        if (string.IsNullOrEmpty(scheme.TenantId))
            throw new InvalidOperationException(
                $"OAuth scheme '{nameof(EntraIdOAuthConfigurator)}' requires a non-empty TenantId.");

        var startupLogger = loggerFactory.CreateLogger<EntraIdOAuthConfigurator>();
        var instance = scheme.Instance ?? "https://login.microsoftonline.com/";
        var authority = $"{instance.TrimEnd('/')}/{scheme.TenantId}/v2.0";
        var audience = scheme.Audience
            ?? scheme.ClientId
            ?? oauth.ServerUrl;

        startupLogger.LogDebug("Configuring EntraId JWT bearer: Authority={Authority}, Audience={Audience}, TenantId={TenantId}",
            authority, audience, scheme.TenantId);

        options.Authority = authority;

        if (scheme.DisableBackchannelSslValidation)
        {
            startupLogger.LogWarning("EntraId OAuth: SSL validation disabled for backchannel requests");
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
            RoleClaimType = "roles"
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var tokenPreview = context.Token is { Length: > 0 } t
                    ? t[..Math.Min(t.Length, 20)] + "..."
                    : "(none)";
                LogFromRequest(context.HttpContext, "EntraId JWT message received: Token={TokenPreview}", tokenPreview);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var name = context.Principal?.Identity?.Name ?? "unknown";
                var oid = context.Principal?.FindFirstValue("oid") ?? "unknown";
                var scopes = context.Principal?.FindAll("scope").Select(c => c.Value).ToList();
                LogFromRequest(context.HttpContext, "EntraId token validated: Name={Name}, Oid={Oid}, Scopes={Scopes}", name, oid, scopes);
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                LogFromRequest(context.HttpContext, context.Exception, "EntraId authentication failed: {Message}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                LogFromRequest(context.HttpContext, "EntraId JWT challenge issued: Error={Error}, Description={ErrorDescription}",
                    context.Error, context.ErrorDescription);
                return Task.CompletedTask;
            }
        };
    }

    private static void LogFromRequest(HttpContext httpContext, string message, params object?[] args)
    {
        var logger = httpContext.RequestServices.GetService<ILogger<EntraIdOAuthConfigurator>>();
        logger?.LogDebug(message, args);
    }

    private static void LogFromRequest(HttpContext httpContext, Exception exception, string message, params object?[] args)
    {
        var logger = httpContext.RequestServices.GetService<ILogger<EntraIdOAuthConfigurator>>();
        logger?.LogWarning(exception, message, args);
    }
}
