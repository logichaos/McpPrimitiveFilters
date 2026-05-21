using Microsoft.AspNetCore.Hosting;
using TUnit.AspNetCore;
using TUnit.Core.Interfaces;

namespace AuthenticatedHttpMcpServer.Integration.Tests;

// Runs the real app with the production JWT + API-key handlers instead of
// TestAuthHandler so integration tests can exercise real authentication logic.
public class RealAuthWebApplicationFactory : TestWebApplicationFactory<Program>, IAsyncInitializer
{
    public Task InitializeAsync()
    {
        _ = Server;
        return Task.CompletedTask;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        // Development mode: RequireSignedTokens = false, so unsigned JWTs are accepted.
        builder.UseEnvironment("Development");
    }
}
