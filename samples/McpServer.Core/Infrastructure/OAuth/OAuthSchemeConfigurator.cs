using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace McpServer.Infrastructure.OAuth;

public interface OAuthSchemeConfigurator
{
  string ProviderType { get; }
  void Configure(JwtBearerOptions options, OAuthSchemeConfig scheme, OAuthOptions oauth, ILoggerFactory loggerFactory);
}