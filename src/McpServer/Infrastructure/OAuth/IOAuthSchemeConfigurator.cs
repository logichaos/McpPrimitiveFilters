using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace McpServer.Infrastructure.OAuth;

public interface IOAuthSchemeConfigurator
{
    string ProviderType { get; }
    void Configure(JwtBearerOptions options, OAuthSchemeConfig scheme, OAuthOptions oauth);
}
