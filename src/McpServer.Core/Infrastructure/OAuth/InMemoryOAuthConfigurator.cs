using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace McpServer.Infrastructure.OAuth;

public sealed class InMemoryOAuthConfigurator : OAuthSchemeConfigurator
{
    public const string ProviderTypeName = "InMemory";
    public string ProviderType => "InMemory";

    public void Configure(JwtBearerOptions options, OAuthSchemeConfig scheme, OAuthOptions oauth, ILoggerFactory loggerFactory)
    {
        if (string.IsNullOrEmpty(scheme.AuthorityUrl))
            throw new InvalidOperationException(
                $"OAuth scheme '{nameof(InMemoryOAuthConfigurator)}' requires a non-empty AuthorityUrl.");

        var startupLogger = loggerFactory.CreateLogger<InMemoryOAuthConfigurator>();
        var authority = scheme.AuthorityUrl;
        var issuer = scheme.Issuer ?? authority;

        var audiences = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrEmpty(scheme.Audience)) audiences.Add(scheme.Audience);
        if (!string.IsNullOrEmpty(oauth.ServerUrl)) audiences.Add(oauth.ServerUrl);

        startupLogger.LogDebug("Configuring InMemory JWT bearer: Authority={Authority}, Audiences={Audiences}, Issuer={Issuer}", authority, audiences, issuer);

        options.Authority = authority;

        if (scheme.DisableBackchannelSslValidation)
        {
            startupLogger.LogWarning("InMemory OAuth: SSL validation disabled for backchannel requests");
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
            ValidAudience = audiences.FirstOrDefault(),
            ValidAudiences = audiences,
            ValidIssuer = issuer,
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
                LogFromRequest(context.HttpContext, "InMemory JWT message received: Token={TokenPreview}", tokenPreview);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var name = context.Principal?.Identity?.Name ?? "unknown";
                var scopes = context.Principal?.FindAll("scope").Select(c => c.Value).ToList();
                LogFromRequest(context.HttpContext, "InMemory token validated: Name={Name}, Scopes={Scopes}", name, scopes);
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                LogFromRequest(context.HttpContext, context.Exception, "InMemory authentication failed: {Message}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                LogFromRequest(context.HttpContext, "InMemory JWT challenge issued: Error={Error}, Description={ErrorDescription}",
                    context.Error, context.ErrorDescription);
                return Task.CompletedTask;
            }
        };
    }

    private static void LogFromRequest(HttpContext httpContext, string message, params object?[] args)
    {
        var logger = httpContext.RequestServices.GetService<ILogger<InMemoryOAuthConfigurator>>();
        logger?.LogDebug(message, args);
    }

    private static void LogFromRequest(HttpContext httpContext, Exception exception, string message, params object?[] args)
    {
        var logger = httpContext.RequestServices.GetService<ILogger<InMemoryOAuthConfigurator>>();
        logger?.LogWarning(exception, message, args);
    }
}
