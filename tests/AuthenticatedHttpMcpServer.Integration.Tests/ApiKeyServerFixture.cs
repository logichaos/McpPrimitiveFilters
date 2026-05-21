using AuthenticatedHttpMcpServer.Infrastructure.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Interfaces;

namespace AuthenticatedHttpMcpServer.Integration.Tests;

// A purpose-built WebApplication that mirrors the production ApiKey auth setup
// (same handler, same options, same policy) but adds a test endpoint so the
// full auth+authorization pipeline can be exercised without going through Program.cs.
// A separate fixture is necessary because WebApplicationFactory<Program> provides no
// hook to call MapGet on the running WebApplication instance.
public sealed class ApiKeyServerFixture : IAsyncInitializer, IAsyncDisposable
{
    private WebApplication? _app;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseTestServer();
        builder.Services.AddLogging();
        builder.Services.AddRouting();

        // Mirror Builder.Auth.cs: same handler type, same key predicate, same policy.
        builder.Services.AddAuthentication("Bearer")
            .AddScheme<ApiKeyAuthHandlerOptions, ApiKeyAuthHandler>(
                "ApiKey-Header",
                opts => opts.ValidateKey = k => k == "Lifetime Subscription");
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("mcp_subscription", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AuthenticationSchemes.Add("ApiKey-Header");
            });

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapGet("/api-key-protected", () => "OK")
            .RequireAuthorization("mcp_subscription");

        await _app.StartAsync();
    }

    public HttpClient CreateClient() => _app!.GetTestClient();

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
            await _app.DisposeAsync();
    }
}
