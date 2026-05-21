using System.Security.Claims;
using AuthenticatedHttpMcpServer;
using Microsoft.AspNetCore.Http;

namespace AuthenticatedHttpMcpServer.Unit.Tests;

public class DemoToolsTests
{
    private static DemoTools CreateTools(ClaimsPrincipal? user = null)
    {
        var context = new DefaultHttpContext();
        if (user is not null)
            context.User = user;
        return new DemoTools(new StubHttpContextAccessor(context));
    }

    private static ClaimsPrincipal AuthenticatedUser(string name) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], "test"));

    [Test]
    public async Task HelloWorld_DefaultName_GreetsWorld()
    {
        var result = CreateTools().HelloWorld();

        await Assert.That(result).IsEqualTo("Hello, world(anonymous) from your MCP server");
    }

    [Test]
    public async Task HelloWorld_CustomName_UsesProvidedName()
    {
        var result = CreateTools().HelloWorld("Alice");

        await Assert.That(result).IsEqualTo("Hello, Alice(anonymous) from your MCP server");
    }

    [Test]
    public async Task HelloWorld_AuthenticatedUser_IncludesIdentityName()
    {
        var result = CreateTools(AuthenticatedUser("bob")).HelloWorld("Alice");

        await Assert.That(result).IsEqualTo("Hello, Alice(bob) from your MCP server");
    }

    [Test]
    public async Task HelloWorld_NullHttpContext_TreatsUserAsAnonymous()
    {
        var tools = new DemoTools(new StubHttpContextAccessor(null));

        var result = tools.HelloWorld("test");

        await Assert.That(result).IsEqualTo("Hello, test(anonymous) from your MCP server");
    }

    [Test]
    public async Task RandomNumber_ReturnedNumberIsWithinRange()
    {
        var result = CreateTools().RandomNumber(10, 100);

        var number = ExtractNumber(result);
        await Assert.That(number).IsGreaterThanOrEqualTo(10).And.IsLessThan(100);
    }

    [Test]
    public async Task RandomNumber_SinglePossibleValue_ReturnsIt()
    {
        // Next(n, n+1) can only return n, making the test deterministic
        var result = CreateTools().RandomNumber(42, 43);

        await Assert.That(ExtractNumber(result)).IsEqualTo(42);
    }

    [Test]
    public async Task RandomNumber_AuthenticatedUser_IncludesIdentityName()
    {
        var result = CreateTools(AuthenticatedUser("carol")).RandomNumber(1, 2);

        await Assert.That(result).StartsWith("Hello Random carol,");
    }

    [Test]
    public async Task RandomNumber_AnonymousUser_ShowsNullForName()
    {
        var result = CreateTools().RandomNumber(1, 2);

        await Assert.That(result).StartsWith("Hello Random ,");
    }

    private static int ExtractNumber(string result)
    {
        // "Hello Random ..., your number is {N}) from your MCP server"
        var start = result.IndexOf("your number is ", StringComparison.Ordinal) + "your number is ".Length;
        var end = result.IndexOf(')', start);
        return int.Parse(result[start..end]);
    }

    private sealed class StubHttpContextAccessor(HttpContext? context) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get => context; set { } }
    }
}
