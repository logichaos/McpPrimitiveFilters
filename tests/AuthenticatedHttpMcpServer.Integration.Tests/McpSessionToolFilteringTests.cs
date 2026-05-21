using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer.Integration.Tests;

public class McpSessionToolFilteringTests
{
    [ClassDataSource<TestWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required TestWebApplicationFactory TestWebApplicationFactory { get; init; }

    private async Task<IReadOnlyList<string>> GetToolNamesForClaims(params Claim[] claims)
    {
        var transportOpts = TestWebApplicationFactory.Services
            .GetRequiredService<IOptions<HttpServerTransportOptions>>().Value;

        using var scope = TestWebApplicationFactory.Services.CreateScope();

        var ctx = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        var mcpOpts = new McpServerOptions();
        await transportOpts.ConfigureSessionOptions!(ctx, mcpOpts, CancellationToken.None);

        return mcpOpts.ToolCollection?.Select(t => t.ProtocolTool.Name).ToList()
            ?? [];
    }

    [Test]
    public async Task ConfigureSessionOptions_WithToolAllScope_ReturnsBothTools()
    {
        var tools = await GetToolNamesForClaims(new Claim("scope", "tool:all"));

        await Assert.That(tools).IsEquivalentTo(["hello_world", "random_number"]);
    }

    [Test]
    public async Task ConfigureSessionOptions_WithSingleToolScope_ReturnsOnlyThatTool()
    {
        var tools = await GetToolNamesForClaims(new Claim("scope", "tool:hello_world"));

        await Assert.That(tools).IsEquivalentTo(["hello_world"]);
    }

    [Test]
    public async Task ConfigureSessionOptions_WithNoScope_ReturnsNoTools()
    {
        var tools = await GetToolNamesForClaims();

        await Assert.That(tools.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ConfigureSessionOptions_WithMultipleToolScopes_ReturnsOnlyNamedTools()
    {
        var tools = await GetToolNamesForClaims(
            new Claim("scope", "tool:hello_world"),
            new Claim("scope", "tool:random_number"));

        await Assert.That(tools).IsEquivalentTo(["hello_world", "random_number"]);
    }
}
