using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;

namespace AuthenticatedHttpMcpServer.Integration.Tests;

// Both strategies are active:
//   ScopeToolsClaimsPrincipalToolSelectionStrategy — filters by scope claims
//   ToolsOptionsToolSelectionStrategy              — filters by AllowedTools config
// A tool must pass BOTH. Development appsettings set AllowedTools: ["random_number"].
public class McpSessionToolFilteringTests
{
    [ClassDataSource<TestWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required TestWebApplicationFactory TestWebApplicationFactory { get; init; }

    private async Task<IReadOnlyList<string>> GetToolNames(params Claim[] claims)
    {
        var transportOpts = TestWebApplicationFactory.Services
            .GetRequiredService<IOptions<HttpServerTransportOptions>>().Value;

        using var scope = TestWebApplicationFactory.Services.CreateScope();

        var ctx = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        // Both strategies use IHttpContextAccessor (AsyncLocal), so wire it here since
        // this test bypasses the middleware pipeline that normally sets it.
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = ctx;

        var mcpOpts = new McpServerOptions();
        await transportOpts.ConfigureSessionOptions!(ctx, mcpOpts, CancellationToken.None);

        return mcpOpts.ToolCollection?.Select(t => t.ProtocolTool.Name).ToList() ?? [];
    }

    [Test]
    public async Task ConfigureSessionOptions_NoScopeClaims_ReturnsNoTools()
    {
        var tools = await GetToolNames();

        await Assert.That(tools.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ConfigureSessionOptions_ToolAllScope_ReturnsOnlyConfigAllowedTool()
    {
        // tool:all satisfies the scope strategy, but AllowedTools: ["random_number"] still limits output.
        var tools = await GetToolNames(new Claim("scope", "tool:all"));

        await Assert.That(tools).IsEquivalentTo(["random_number"]);
    }

    [Test]
    public async Task ConfigureSessionOptions_ScopeMatchesAllowedTool_ReturnsThatTool()
    {
        var tools = await GetToolNames(new Claim("scope", "tool:random_number"));

        await Assert.That(tools).IsEquivalentTo(["random_number"]);
    }

    [Test]
    public async Task ConfigureSessionOptions_ScopeMatchesToolNotInAllowedList_ReturnsNoTools()
    {
        // hello_world is in scope but not in AllowedTools — intersection is empty.
        var tools = await GetToolNames(new Claim("scope", "tool:hello_world"));

        await Assert.That(tools.Count).IsEqualTo(0);
    }
}

// AllowedTools overridden to null → options strategy passes everything through;
// only scope claims determine which tools are returned.
public class McpSessionToolFiltering_NullAllowedTools_Tests
{
    [ClassDataSource<AllToolsWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required AllToolsWebApplicationFactory Factory { get; init; }

    private async Task<IReadOnlyList<string>> GetToolNames(params Claim[] claims)
    {
        var transportOpts = Factory.Services
            .GetRequiredService<IOptions<HttpServerTransportOptions>>().Value;

        using var scope = Factory.Services.CreateScope();

        var ctx = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = ctx;

        var mcpOpts = new McpServerOptions();
        await transportOpts.ConfigureSessionOptions!(ctx, mcpOpts, CancellationToken.None);

        return mcpOpts.ToolCollection?.Select(t => t.ProtocolTool.Name).ToList() ?? [];
    }

    [Test]
    public async Task ConfigureSessionOptions_ToolAllScope_ReturnsAllRegisteredTools()
    {
        var tools = await GetToolNames(new Claim("scope", "tool:all"));

        await Assert.That(tools).IsEquivalentTo(["hello_world", "random_number"]);
    }

    [Test]
    public async Task ConfigureSessionOptions_SingleToolScope_ReturnsOnlyThatTool()
    {
        var tools = await GetToolNames(new Claim("scope", "tool:hello_world"));

        await Assert.That(tools).IsEquivalentTo(["hello_world"]);
    }

    [Test]
    public async Task ConfigureSessionOptions_NoScopeClaims_ReturnsNoTools()
    {
        var tools = await GetToolNames();

        await Assert.That(tools.Count).IsEqualTo(0);
    }
}

// AllowedTools overridden to [] → options strategy blocks everything regardless of claims.
public class McpSessionToolFiltering_EmptyAllowedTools_Tests
{
    [ClassDataSource<EmptyAllowedToolsWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required EmptyAllowedToolsWebApplicationFactory Factory { get; init; }

    private async Task<IReadOnlyList<string>> GetToolNames(params Claim[] claims)
    {
        var transportOpts = Factory.Services
            .GetRequiredService<IOptions<HttpServerTransportOptions>>().Value;

        using var scope = Factory.Services.CreateScope();

        var ctx = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));

        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = ctx;

        var mcpOpts = new McpServerOptions();
        await transportOpts.ConfigureSessionOptions!(ctx, mcpOpts, CancellationToken.None);

        return mcpOpts.ToolCollection?.Select(t => t.ProtocolTool.Name).ToList() ?? [];
    }

    [Test]
    public async Task ConfigureSessionOptions_EmptyAllowedTools_WithToolAllScope_ReturnsAllTools()
    {
        var tools = await GetToolNames(new Claim("scope", "tool:all"));

        await Assert.That(tools.Count).IsEqualTo(2);
    }
}
