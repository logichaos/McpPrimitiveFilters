using McpServer.Tools;

namespace McpServer.Unit.Tests;

public class DemoToolsTests
{
    private readonly RandomNumberTools _sut = new();

    [Test]
    public async Task GetTimestamp_ReturnsValidIso8601()
    {
        var result = _sut.GetTimestamp();

        await Assert.That(DateTimeOffset.TryParse(result, out var parsed)).IsTrue();
        await Assert.That(result.EndsWith("Z") || result.Contains('+')).IsTrue();
    }

    [Test]
    public async Task Echo_ReturnsInputMessage()
    {
        var result = _sut.Echo("Hello, World!");

        await Assert.That(result).IsEqualTo("Hello, World!");
    }

    [Test]
    public async Task Echo_ReturnsEmptyStringForEmptyInput()
    {
        var result = _sut.Echo("");

        await Assert.That(result).IsEqualTo("");
    }

    [Test]
    public async Task ListUsers_ReturnsAtLeastThreeUsers()
    {
        var result = _sut.ListUsers();

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsGreaterThanOrEqualTo(3);
    }

    [Test]
    public async Task ListUsers_EachUserHasNonEmptyFields()
    {
        var result = _sut.ListUsers();

        foreach (var user in result)
        {
            await Assert.That(user.Username).IsNotNull().And.IsNotEmpty();
            await Assert.That(user.Role).IsNotNull().And.IsNotEmpty();
        }
    }

    [Test]
    public async Task GetServerStats_ReturnsValidStats()
    {
        var result = _sut.GetServerStats();

        await Assert.That(result.Uptime).IsNotNull().And.IsNotEmpty();
        await Assert.That(result.RequestCount).IsGreaterThanOrEqualTo(0);
    }
}
