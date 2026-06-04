using McpServer.Infrastructure.OAuth;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Net.Http.Headers;

using ModelContextProtocol.AspNetCore.Authentication;

namespace McpServer.Infrastructure;

public static partial class ApiBuilder
{
    private const string McpCorsPolicyName = "McpBrowserClient";

    internal sealed class OAuthMarker;

    public static bool IsOAuthConfigured(this IServiceProvider services) =>
        services.GetService<OAuthMarker>() is not null;

    private static readonly Dictionary<string, OAuthSchemeConfigurator> _configurators = new(StringComparer.OrdinalIgnoreCase)
    {
        [InMemoryOAuthConfigurator.ProviderTypeName] = new InMemoryOAuthConfigurator(),
        [EntraIdOAuthConfigurator.ProviderTypeName] = new EntraIdOAuthConfigurator(),
        [Auth0OAuthConfigurator.ProviderTypeName] = new Auth0OAuthConfigurator(),
    };

    public static void RegisterOAuthConfigurator(OAuthSchemeConfigurator configurator)
    {
        _configurators[configurator.ProviderType] = configurator;
    }

    public static IServiceCollection AddOAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var loggerFactory = NullLoggerFactory.Instance;
        var logger = loggerFactory.CreateLogger("McpServer.OAuth");

        var oauthSection = configuration.GetSection(OAuthOptions.SectionName);
        if (!oauthSection.Exists())
        {
            logger.LogDebug("OAuth section '{Section}' not found — auth disabled", OAuthOptions.SectionName);
            return services;
        }

        var oauth = oauthSection.Get<OAuthOptions>()!;

        if (oauth.Schemes.Count == 0)
        {
            logger.LogWarning("OAuth section exists but has no schemes — auth disabled");
            return services;
        }

        var enabledSchemes = oauth.Schemes
            .Where(kvp => kvp.Value.Enabled)
            .ToList();

        if (enabledSchemes.Count == 0)
        {
            logger.LogWarning("No OAuth schemes are enabled — auth disabled");
            return services;
        }

        logger.LogInformation("OAuth enabled with {SchemeCount} scheme(s): {Schemes}",
            enabledSchemes.Count,
            enabledSchemes.Select(s => $"{s.Key} ({s.Value.Type})"));

        logger.LogDebug("OAuth config: DefaultScheme={DefaultScheme}, ServerUrl={ServerUrl}, ScopesSupported={Scopes}",
            oauth.DefaultScheme, oauth.ServerUrl, oauth.ScopesSupported);

        ValidateOptions(oauth, enabledSchemes);

        services.AddSingleton(new OAuthMarker());

        var allowedOrigins = configuration
            .GetSection("Mcp:AllowedOrigins")
            .Get<string[]>() ?? ["http://localhost:5173", "http://localhost:6274"];

        services.AddCors(options =>
        {
            options.AddPolicy(McpCorsPolicyName, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .WithMethods("POST", "GET", "DELETE", "OPTIONS")
                    .WithHeaders(HeaderNames.ContentType, HeaderNames.Authorization, "MCP-Protocol-Version")
                    .WithExposedHeaders(HeaderNames.WWWAuthenticate, "Mcp-Session-Id");
            });
        });

        var authBuilder = services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;

            if (enabledSchemes.Count == 1)
                options.DefaultAuthenticateScheme = enabledSchemes[0].Key;
            else
                options.DefaultAuthenticateScheme = "MultiScheme";
        });

        if (enabledSchemes.Count > 1)
        {
            logger.LogInformation("Multiple schemes enabled — using MultiScheme policy scheme");
            authBuilder.AddPolicyScheme("MultiScheme", "Multi-Scheme JWT Bearer", options =>
            {
                options.ForwardDefaultSelector = _ => enabledSchemes[0].Key;
            });
        }

        var authorizationServers = new List<string>();

        foreach (var (schemeName, schemeConfig) in enabledSchemes)
        {
            if (!_configurators.TryGetValue(schemeConfig.Type, out var configurator))
            {
                throw new InvalidOperationException(
                    $"Unknown OAuth provider type '{schemeConfig.Type}' for scheme '{schemeName}'. " +
                    $"Registered types: {string.Join(", ", _configurators.Keys)}.");
            }

            var authority = ResolveAuthority(schemeConfig);
            if (authority is not null)
            {
                logger.LogDebug("Scheme '{Scheme}': resolved authority = {Authority}", schemeName, authority);
                authorizationServers.Add(authority);
            }
            else
            {
                logger.LogDebug("Scheme '{Scheme}': no authority resolved, using auto-discovery", schemeName);
            }

            authBuilder.AddJwtBearer(schemeName, options =>
            {
                configurator.Configure(options, schemeConfig, oauth, loggerFactory);
            });
        }

        authBuilder.AddMcp(mcpOptions =>
        {
            mcpOptions.ResourceMetadata = new()
            {
                Resource = oauth.Resource,
                ResourceDocumentation = oauth.ResourceDocumentation,
                ScopesSupported = oauth.ScopesSupported ?? [],
            };

            foreach (var server in authorizationServers)
                mcpOptions.ResourceMetadata.AuthorizationServers.Add(server);
        });

        services.AddAuthorization();
        logger.LogInformation("OAuth setup complete: {AuthServerCount} authorization server(s), CORS origins={Origins}",
            authorizationServers.Count, allowedOrigins);

        return services;
    }

    public static WebApplication UseOAuth(this WebApplication app)
    {
        if (!app.Services.IsOAuthConfigured())
            return app;

        app.UseCors(McpCorsPolicyName);
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    private static void ValidateOptions(OAuthOptions oauth, List<KeyValuePair<string, OAuthSchemeConfig>> enabledSchemes)
    {
        if (string.IsNullOrEmpty(oauth.DefaultScheme))
        {
            throw new InvalidOperationException(
                $"'{OAuthOptions.SectionName}:DefaultScheme' must be set.");
        }

        if (!enabledSchemes.Any(s => s.Key == oauth.DefaultScheme))
        {
            throw new InvalidOperationException(
                $"DefaultScheme '{oauth.DefaultScheme}' is not an enabled scheme. " +
                $"Enabled schemes: {string.Join(", ", enabledSchemes.Select(s => s.Key))}");
        }
    }

    private static string? ResolveAuthority(OAuthSchemeConfig scheme)
    {
        if (!string.IsNullOrEmpty(scheme.AuthorityUrl))
            return scheme.AuthorityUrl;

        if (scheme.Type.Equals("EntraId", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(scheme.TenantId))
        {
            var instance = scheme.Instance ?? "https://login.microsoftonline.com/";
            return $"{instance.TrimEnd('/')}/{scheme.TenantId}/v2.0";
        }

        if (scheme.Type.Equals("Auth0", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(scheme.Domain))
        {
            return $"https://{scheme.Domain.TrimEnd('/')}/";
        }

        return null;
    }
}
