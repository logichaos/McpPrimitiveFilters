using AuthenticatedHttpMcpServer.Infrastructure.Authentication;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AuthenticatedHttpMcpServer.Infrastructure;

public static partial class ApiBuilder
{
  public static IServiceCollection AddAuthServices(this IServiceCollection services, IHostEnvironment environment)
  {
    IConfigurationManager<OpenIdConnectConfiguration>? oidcConfigManager = null;
    if (!environment.IsDevelopment())
    {
      var tenantId = GlobalConfigurations.ApiSettings?.EntraId?.TenantId ?? "entraid";
      var authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
      oidcConfigManager = new ConfigurationManager<OpenIdConnectConfiguration>(
        $"{authority}/.well-known/openid-configuration",
        new OpenIdConnectConfigurationRetriever());
    }

    services.AddAuthentication(Constants.Auth.Schemes.Bearer)
      .AddScheme<JwtBearerAuthHandlerOptions, JwtBearerAuthHandler>(
        Constants.Auth.Schemes.Bearer, opts =>
        {
          opts.ValidAudiences = GlobalConfigurations.ApiSettings?.TokenValidation.ValidAudiences;
          opts.ValidIssuers = GlobalConfigurations.ApiSettings?.TokenValidation.ValidIssuers;
          opts.RequireSignedTokens = !environment.IsDevelopment();
          opts.OidcConfigurationManager = oidcConfigManager;
        })
      .AddScheme<ApiKeyAuthHandlerOptions, ApiKeyAuthHandler>(
        Constants.Auth.Schemes.ApiKey, opts =>
        {
          opts.ValidateKey = key => key == "Lifetime Subscription";
        });

    services.AddAuthorizationBuilder()
      .AddPolicy(Constants.Auth.Policies.MrAwesome, policy =>
      {
        policy.RequireAuthenticatedUser();
        policy.AuthenticationSchemes.Add(Constants.Auth.Schemes.Bearer);
        policy.RequireRole(Constants.Auth.Roles.McpCaller, Constants.Auth.Roles.Awesome);
      })
      .AddPolicy(Constants.Auth.Policies.McpSubscription, policy =>
      {
        policy.RequireAuthenticatedUser();
        policy.AuthenticationSchemes.Add(Constants.Auth.Schemes.ApiKey);
      });

    return services;
  }
}
