using System.Security.Claims;
using AuthenticatedHttpMcpServer.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;

namespace AuthenticatedHttpMcpServer.Infrastructure;

public static partial class ApiBuilder
{
  public static IServiceCollection AddAuthServices(this IServiceCollection services, IHostEnvironment environment)
  {
    var authBuilder = services.AddAuthentication(options =>
    {
      // McpAuth handles 401 challenges with WWW-Authenticate + resource_metadata per MCP spec.
      options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
      // JWT is the primary authentication scheme.
      options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    });

    // ── JWT Bearer ──
    authBuilder.AddJwtBearer(options =>
    {
      if (environment.IsDevelopment())
      {
        // Development: use the TestOAuthServer as the authority.
        options.Authority = "https://localhost:7029";
        options.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateLifetime = true,
          ValidateIssuerSigningKey = true,
          // Accept tokens issued for any of our server URLs.
          ValidAudiences = ["http://localhost:5105", "https://localhost:7093"],
        };
      }
      else
      {
        // Production: use EntraId as the authority.
        var entra = GlobalConfigurations.ApiSettings?.EntraId;
        var oauth = GlobalConfigurations.ApiSettings?.OAuth;
        var tenantId = entra?.TenantId ?? "common";
        options.Authority = oauth?.Authority
                            ?? $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuer = true,
          ValidateAudience = true,
          ValidateLifetime = true,
          ValidateIssuerSigningKey = true,
          ValidAudiences = oauth?.ValidAudiences,
          ValidIssuers = oauth?.ValidIssuers,
        };
      }

      // Keep claims as-is so that "scope" and "allowed_tools" stay accessible
      // without remapping. We manually split the "roles" claim in OnTokenValidated.
      options.MapInboundClaims = false;

      // Tell ASP.NET to use the "name" claim for Identity.Name.
      options.TokenValidationParameters.NameClaimType = "name";

      options.Events = new JwtBearerEvents
      {
        OnTokenValidated = context =>
        {
          var identity = (ClaimsIdentity)context.Principal!.Identity!;

          // The TestOAuthServer emits a single space-separated "roles" string.
          // Split it into individual Role claims so RequireRole() works.
          var rolesClaim = identity.FindFirst("roles");
          if (rolesClaim?.Value is { Length: > 0 } roles)
          {
            foreach (var role in roles.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
              identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
          }

          var name = context.Principal?.Identity?.Name ?? "unknown";
          Console.WriteLine($"✅ JWT validated for: {name}");
          return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
          Console.WriteLine($"❌ JWT authentication failed: {context.Exception.Message}");
          return Task.CompletedTask;
        },
      };
    });

    // ── MCP Auth Handler ──
    // Serves /.well-known/oauth-protected-resource and issues correct WWW-Authenticate challenges.
    authBuilder.AddMcp(options =>
    {
      options.ResourceMetadata = new()
      {
        ResourceDocumentation = "https://docs.example.com/mcp-server",
        AuthorizationServers =
        {
          environment.IsDevelopment()
            ? "https://localhost:7029"
            : (GlobalConfigurations.ApiSettings?.OAuth?.Authority ?? "https://login.microsoftonline.com/tenant-id-here/v2.0")
        },
        ScopesSupported = ["mcp:tools"],
      };
    });

    // ── API Key (development only) ──
    if (environment.IsDevelopment())
    {
      var devApiKey = GlobalConfigurations.ApiSettings?.ApiKey;
      if (!string.IsNullOrWhiteSpace(devApiKey))
      {
        authBuilder.AddScheme<ApiKeyAuthHandlerOptions, ApiKeyAuthHandler>(
          Constants.Auth.Schemes.ApiKey, opts =>
          {
            opts.ValidateKey = key => key == devApiKey;
          });
      }
    }

    // ── Authorization Policies ──
    services.AddAuthorizationBuilder()
      // "MCP": accepts either JWT (Bearer) or API key
      .AddPolicy(Constants.Auth.Policies.Mcp, policy =>
      {
        policy.RequireAuthenticatedUser();
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.AuthenticationSchemes.Add(Constants.Auth.Schemes.ApiKey);
      })
      // "mrawesome": JWT + specific roles
      .AddPolicy(Constants.Auth.Policies.MrAwesome, policy =>
      {
        policy.RequireAuthenticatedUser();
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireRole(Constants.Auth.Roles.McpCaller, Constants.Auth.Roles.Awesome);
      })
      // "mcp_subscription": API key only
      .AddPolicy(Constants.Auth.Policies.McpSubscription, policy =>
      {
        policy.RequireAuthenticatedUser();
        policy.AuthenticationSchemes.Add(Constants.Auth.Schemes.ApiKey);
      });

    return services;
  }
}
