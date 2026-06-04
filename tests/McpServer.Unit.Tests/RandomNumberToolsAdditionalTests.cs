using McpServer.Tools;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Unit.Tests;

public class RandomNumberToolsAdditionalTests
{
    private readonly RandomNumberTools _sut = new(NullLogger<RandomNumberTools>.Instance);

    [Test]
    public async Task GetRandomNumber_ReturnsValueInRange()
    {
        for (int i = 0; i < 100; i++)
        {
            var result = _sut.GetRandomNumber(min: 10, max: 20);
            await Assert.That(result).IsGreaterThanOrEqualTo(10);
            await Assert.That(result).IsLessThan(20);
        }
    }

    [Test]
    public async Task GetRandomNumber_DefaultRange_0To100()
    {
        for (int i = 0; i < 50; i++)
        {
            var result = _sut.GetRandomNumber();
            await Assert.That(result).IsGreaterThanOrEqualTo(0);
            await Assert.That(result).IsLessThan(100);
        }
    }

    [Test]
    public async Task GetRandomNumber_NegativeRange()
    {
        for (int i = 0; i < 50; i++)
        {
            var result = _sut.GetRandomNumber(min: -50, max: -10);
            await Assert.That(result).IsGreaterThanOrEqualTo(-50);
            await Assert.That(result).IsLessThan(-10);
        }
    }

    [Test]
    public async Task GetRandomNumber_SingleElementRange_ReturnsMin()
    {
        var result = _sut.GetRandomNumber(min: 5, max: 6);
        await Assert.That(result).IsEqualTo(5);
    }

    [Test]
    public async Task GetRandomNumber_ReversedRange_ThrowsArgumentOutOfRange()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
        {
            _sut.GetRandomNumber(min: 20, max: 10);
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task DemoUser_Record_IsValueEqual()
    {
        var a = new DemoUser("alice", "admin");
        var b = new DemoUser("alice", "admin");

        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task ServerStats_Record_HasExpectedValues()
    {
        var stats = new ServerStats("1 day", 100);

        await Assert.That(stats.Uptime).IsEqualTo("1 day");
        await Assert.That(stats.RequestCount).IsEqualTo(100);
    }
}
