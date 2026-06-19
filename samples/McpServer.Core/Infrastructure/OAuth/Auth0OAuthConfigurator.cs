using System.Security.Claims;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace McpServer.Infrastructure.OAuth;

public sealed class Auth0OAuthConfigurator : OAuthSchemeConfigurator
{
  public const string ProviderTypeName = "Auth0";
  public string ProviderType => "Auth0";

  public void Configure(JwtBearerOptions options, OAuthSchemeConfig scheme, OAuthOptions oauth, ILoggerFactory loggerFactory)
  {
    if (string.IsNullOrEmpty(scheme.Domain))
      throw new InvalidOperationException(
          $"OAuth scheme '{nameof(Auth0OAuthConfigurator)}' requires a non-empty Domain.");

    var startupLogger = loggerFactory.CreateLogger<Auth0OAuthConfigurator>();
    var authority = $"https://{scheme.Domain.TrimEnd('/')}/";
    var audience = scheme.Audience
        ?? scheme.ClientId
        ?? oauth.ServerUrl;

    startupLogger.LogDebug("Configuring Auth0 JWT bearer: Authority={Authority}, Audience={Audience}, Domain={Domain}",
        authority, audience, scheme.Domain);

    options.Authority = authority;

    if (scheme.DisableBackchannelSslValidation)
    {
      startupLogger.LogWarning("Auth0 OAuth: SSL validation disabled for backchannel requests");
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
      RoleClaimType = "https://schemas.example.com/roles"
    };

    options.Events = new JwtBearerEvents
    {
      OnMessageReceived = context =>
      {
        var tokenPreview = context.Token is { Length: > 0 } t
                  ? t[..Math.Min(t.Length, 20)] + "..."
                  : "(none)";
        LogFromRequest(context.HttpContext, "Auth0 JWT message received: Token={TokenPreview}", tokenPreview);
        return Task.CompletedTask;
      },
      OnTokenValidated = context =>
      {
        var name = context.Principal?.Identity?.Name ?? "unknown";
        var sub = context.Principal?.FindFirstValue("sub") ?? "unknown";
        var scopes = context.Principal?.FindAll("scope").Select(c => c.Value).ToList();
        LogFromRequest(context.HttpContext, "Auth0 token validated: Name={Name}, Sub={Sub}, Scopes={Scopes}", name, sub, scopes);
        return Task.CompletedTask;
      },
      OnAuthenticationFailed = context =>
      {
        LogFromRequest(context.HttpContext, context.Exception, "Auth0 authentication failed: {Message}", context.Exception.Message);
        return Task.CompletedTask;
      },
      OnChallenge = context =>
      {
        LogFromRequest(context.HttpContext, "Auth0 JWT challenge issued: Error={Error}, Description={ErrorDescription}",
                  context.Error, context.ErrorDescription);
        return Task.CompletedTask;
      }
    };
  }

  private static void LogFromRequest(HttpContext httpContext, string message, params object?[] args)
  {
    var logger = httpContext.RequestServices.GetService<ILogger<Auth0OAuthConfigurator>>();
    logger?.LogDebug(message, args);
  }

  private static void LogFromRequest(HttpContext httpContext, Exception exception, string message, params object?[] args)
  {
    var logger = httpContext.RequestServices.GetService<ILogger<Auth0OAuthConfigurator>>();
    logger?.LogWarning(exception, message, args);
  }
}