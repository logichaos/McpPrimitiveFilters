using System.Security.Claims;
using System.Text;
using AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TUnit.AspNetCore;
using TUnit.Core.Interfaces;

namespace AuthenticatedHttpMcpServer.Integration.Tests;

/// <summary>
/// Test factory that reconfigures the JWT Bearer handler to accept tokens signed
/// with a local test key (HS256) instead of requiring the TestOAuthServer.
/// The authorization policies are left intact so policy enforcement is tested.
/// </summary>
public class TestWebApplicationFactory : TestWebApplicationFactory<Program>, IAsyncInitializer
{
    public static readonly SymmetricSecurityKey TestSigningKey =
        new(Encoding.UTF8.GetBytes("test-signing-key-at-least-32-chars-long"));

    public Task InitializeAsync()
    {
        _ = Server;
        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ToolsSelection:AllowedTools:0"] = "random_number"
            }));

        builder.ConfigureTestServices(services =>
        {
            // Reconfigure JwtBearer to accept locally-signed test tokens.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // Disable authority-based OIDC discovery (no TestOAuthServer needed).
                options.Authority = null;
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = TestSigningKey,
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role,
                };

                // Don't remap claims — we set RoleClaimType above.
                options.MapInboundClaims = false;
            });
        });
    }

    /// <summary>
    /// Helper to create a valid test JWT with the given claims.
    /// </summary>
    public static string CreateTestToken(IEnumerable<Claim>? claims = null)
    {
        var handler = new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler();
        return handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims ?? [new Claim(ClaimTypes.Name, "test-user")]),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(TestSigningKey, SecurityAlgorithms.HmacSha256),
        });
    }
}

// AllowedTools = null → strategy passes all tools through.
public class AllToolsWebApplicationFactory : TestWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
            services.PostConfigure<ToolsSelectionOptions>(opts => opts.AllowedTools = null));
    }
}

// AllowedTools = [] → strategy blocks all tools.
public class EmptyAllowedToolsWebApplicationFactory : TestWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
            services.PostConfigure<ToolsSelectionOptions>(opts => opts.AllowedTools = []));
    }
}
