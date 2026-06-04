using System.Text.Json;
using FakeItEasy;
using McpServer.Resources;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using McpSvr = ModelContextProtocol.Server.McpServer;

namespace McpServer.Unit.Tests;

public class DemoResourcesTests
{
    private readonly DemoResources _sut = new();

    private static RequestContext<ReadResourceRequestParams> CreateRequestContext(string uri)
    {
        return new RequestContext<ReadResourceRequestParams>(
            A.Fake<McpSvr>(),
            new JsonRpcRequest { Method = "resources/read", Id = new RequestId("test") },
            new ReadResourceRequestParams { Uri = uri });
    }

    [Test]
    public async Task GetServerInfo_ReturnsValidJson()
    {
        var ctx = CreateRequestContext("server://info");
        var result = _sut.GetServerInfo(ctx);

        await Assert.That(result.Uri).IsEqualTo("server://info");
        await Assert.That(result.MimeType).IsEqualTo("application/json");

        var info = JsonSerializer.Deserialize<SystemInfo>(result.Text);
        await Assert.That(info).IsNotNull();
        await Assert.That(info!.MachineName).IsNotEmpty();
        await Assert.That(info.OsDescription).IsNotEmpty();
        await Assert.That(info.RuntimeVersion).IsNotEmpty();
        await Assert.That(info.Uptime).IsNotEmpty();
    }

    [Test]
    public async Task GetServerInfo_ReturnsSameStartedAtOnRepeatedCalls()
    {
        var ctx1 = CreateRequestContext("server://info");
        var ctx2 = CreateRequestContext("server://info");

        var info1 = JsonSerializer.Deserialize<SystemInfo>(_sut.GetServerInfo(ctx1).Text)!;
        var info2 = JsonSerializer.Deserialize<SystemInfo>(_sut.GetServerInfo(ctx2).Text)!;

        await Assert.That(info1.StartedAt).IsEqualTo(info2.StartedAt);
    }

    [Test]
    public async Task GetWeather_ReturnsWeatherForKnownCity()
    {
        var ctx = CreateRequestContext("weather://London");
        var result = _sut.GetWeather(ctx, "London");

        await Assert.That(result.Uri).IsEqualTo("weather://London");
        await Assert.That(result.MimeType).IsEqualTo("application/json");

        var weather = JsonSerializer.Deserialize<WeatherInfo>(result.Text);
        await Assert.That(weather).IsNotNull();
        await Assert.That(weather!.City).IsEqualTo("London");
        await Assert.That(weather.Condition).IsNotEmpty();
        await Assert.That(weather.TemperatureCelsius).IsGreaterThanOrEqualTo(10).And.IsLessThanOrEqualTo(34);
        await Assert.That(weather.Humidity).IsGreaterThanOrEqualTo(30).And.IsLessThanOrEqualTo(79);
        await Assert.That(weather.WindSpeedKmh).IsGreaterThanOrEqualTo(5).And.IsLessThanOrEqualTo(34);
    }

    [Test]
    public async Task GetWeather_SameCityReturnsDeterministicWeather()
    {
        var ctx1 = CreateRequestContext("weather://Tokyo");
        var ctx2 = CreateRequestContext("weather://Tokyo");

        var weather1 = JsonSerializer.Deserialize<WeatherInfo>(_sut.GetWeather(ctx1, "Tokyo").Text)!;
        var weather2 = JsonSerializer.Deserialize<WeatherInfo>(_sut.GetWeather(ctx2, "Tokyo").Text)!;

        await Assert.That(weather1.Condition).IsEqualTo(weather2.Condition);
        await Assert.That(weather1.TemperatureCelsius).IsEqualTo(weather2.TemperatureCelsius);
        await Assert.That(weather1.Humidity).IsEqualTo(weather2.Humidity);
    }

    [Test]
    public async Task GetWeather_DifferentCitiesMayDiffer()
    {
        var ctx1 = CreateRequestContext("weather://Paris");
        var ctx2 = CreateRequestContext("weather://Berlin");

        var weather1 = _sut.GetWeather(ctx1, "Paris");
        var weather2 = _sut.GetWeather(ctx2, "Berlin");

        await Assert.That(weather1.Text).IsNotEqualTo(weather2.Text);
    }

    [Test]
    public async Task GetProcessInfo_ReturnsCurrentProcessData()
    {
        var ctx = CreateRequestContext("system://process-info");
        var result = _sut.GetProcessInfo(ctx);

        await Assert.That(result.Uri).IsEqualTo("system://process-info");
        await Assert.That(result.MimeType).IsEqualTo("application/json");

        var proc = JsonSerializer.Deserialize<ProcessInfo>(result.Text);
        await Assert.That(proc).IsNotNull();
        await Assert.That(proc!.ProcessId).IsGreaterThan(0);
        await Assert.That(proc.ProcessName).IsNotEmpty();
        await Assert.That(proc.ThreadCount).IsGreaterThan(0);
        await Assert.That(proc.HandleCount).IsGreaterThan(0);
    }

    [Test]
    public async Task GetCurrentTime_Iso_ReturnsIso8601Format()
    {
        var ctx = CreateRequestContext("time://iso");
        var result = _sut.GetCurrentTime(ctx, "iso");

        await Assert.That(result.Uri).IsEqualTo("time://iso");
        await Assert.That(result.MimeType).IsEqualTo("text/plain");
        await Assert.That(DateTimeOffset.TryParse(result.Text, out _)).IsTrue();
    }

    [Test]
    public async Task GetCurrentTime_Unix_ReturnsIntegerTimestamp()
    {
        var ctx = CreateRequestContext("time://unix");
        var result = _sut.GetCurrentTime(ctx, "unix");

        await Assert.That(long.TryParse(result.Text, out var ts)).IsTrue();
        await Assert.That(ts).IsGreaterThan(0);
    }

    [Test]
    public async Task GetCurrentTime_Rfc_ReturnsRfc1123Format()
    {
        var ctx = CreateRequestContext("time://rfc");
        var result = _sut.GetCurrentTime(ctx, "rfc");

        await Assert.That(DateTimeOffset.TryParse(result.Text, out _)).IsTrue();
    }

    [Test]
    public async Task GetCurrentTime_Ticks_ReturnsLongInteger()
    {
        var ctx = CreateRequestContext("time://ticks");
        var result = _sut.GetCurrentTime(ctx, "ticks");

        await Assert.That(long.TryParse(result.Text, out var ticks)).IsTrue();
        await Assert.That(ticks).IsGreaterThan(0);
    }

    [Test]
    public async Task GetCurrentTime_UnknownFormat_ThrowsMcpException()
    {
        var ctx = CreateRequestContext("time://invalid");

        await Assert.That(() => _sut.GetCurrentTime(ctx, "invalid"))
            .Throws<McpException>()
            .WithMessageContaining("Unknown format");
    }
}
