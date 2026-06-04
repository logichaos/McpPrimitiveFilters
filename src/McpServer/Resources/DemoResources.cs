using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpServer.Resources;

public record WeatherInfo(string City, string Condition, double TemperatureCelsius, int Humidity, double WindSpeedKmh);

public record ProcessInfo(
    int ProcessId,
    string ProcessName,
    long WorkingSetMb,
    long PeakWorkingSetMb,
    int ThreadCount,
    int HandleCount,
    string StartTime,
    string TotalProcessorTime);

public record SystemInfo(
    string MachineName,
    string OsDescription,
    string RuntimeVersion,
    DateTimeOffset StartedAt,
    string Uptime);

internal static partial class ResourceLogMessages
{
    [LoggerMessage(Level = LogLevel.Debug,
        Message = "System info: MachineName={MachineName}, OS={OsDescription}, Runtime={RuntimeVersion}")]
    public static partial void LogSystemInfo(
        ILogger logger,
        [McpServer.Infrastructure.Compliance.SensitiveData] string machineName,
        string osDescription,
        string runtimeVersion);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Process info: PID={ProcessId}, Name={ProcessName}, WSet={WorkingSetMb}MB, Threads={ThreadCount}, Handles={HandleCount}")]
    public static partial void LogProcessInfo(
        ILogger logger,
        [McpServer.Infrastructure.Compliance.SensitiveData] int processId,
        [McpServer.Infrastructure.Compliance.SensitiveData] string processName,
        long workingSetMb,
        int threadCount,
        int handleCount);
}

[McpServerResourceType]
public class DemoResources
{
    private static readonly DateTimeOffset StartedAt = DateTimeOffset.UtcNow;
    private readonly ILogger<DemoResources> _logger;

    public DemoResources(ILogger<DemoResources> logger)
    {
        _logger = logger;
    }

    [McpServerResource(
        UriTemplate = "server://info",
        Name = "Server Info",
        MimeType = "application/json")]
    [Description("Returns runtime information about the MCP server process.")]
    public TextResourceContents GetServerInfo(RequestContext<ReadResourceRequestParams> requestContext)
    {
        var info = new SystemInfo(
            MachineName: Environment.MachineName,
            OsDescription: $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})",
            RuntimeVersion: RuntimeInformation.FrameworkDescription,
            StartedAt: StartedAt,
            Uptime: (DateTimeOffset.UtcNow - StartedAt).ToString(@"d\d\ h\h\ m\m\ s\s"));

        ResourceLogMessages.LogSystemInfo(_logger, info.MachineName, info.OsDescription, info.RuntimeVersion);

        return new TextResourceContents
        {
            Uri = requestContext.Params.Uri,
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(info, McpResourcesJsonContext.Default.SystemInfo)
        };
    }

    [McpServerResource(
        UriTemplate = "weather://{city}",
        Name = "City Weather",
        MimeType = "application/json")]
    [Description("Returns simulated weather conditions for a given city.")]
    public TextResourceContents GetWeather(
        ModelContextProtocol.Server.McpServer server,
        RequestContext<ReadResourceRequestParams> requestContext,
        [Description("The city name to get weather for")] string city)
    {
        var conditions = new[] { "Sunny", "Cloudy", "Rainy", "Partly Cloudy", "Windy" };
        var hash = Math.Abs(city.GetHashCode(StringComparison.OrdinalIgnoreCase));

        var weather = new WeatherInfo(
            City: city,
            Condition: conditions[hash % conditions.Length],
            TemperatureCelsius: 10 + (hash % 25),
            Humidity: 30 + (hash % 50),
            WindSpeedKmh: 5 + (hash % 30));

        return new TextResourceContents
        {
            Uri = requestContext.Params.Uri,
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(weather, McpResourcesJsonContext.Default.WeatherInfo)
        };
    }

    [McpServerResource(
        UriTemplate = "system://process-info",
        Name = "Process Info",
        MimeType = "application/json")]
    [Description("Returns current process memory and thread information. Changes on every call.")]
    public TextResourceContents GetProcessInfo(RequestContext<ReadResourceRequestParams> requestContext)
    {
        using var proc = Process.GetCurrentProcess();
        var info = new ProcessInfo(
            ProcessId: proc.Id,
            ProcessName: proc.ProcessName,
            WorkingSetMb: proc.WorkingSet64 / (1024 * 1024),
            PeakWorkingSetMb: proc.PeakWorkingSet64 / (1024 * 1024),
            ThreadCount: proc.Threads.Count,
            HandleCount: proc.HandleCount,
            StartTime: proc.StartTime.ToString("O"),
            TotalProcessorTime: proc.TotalProcessorTime.ToString());

        ResourceLogMessages.LogProcessInfo(
            _logger,
            info.ProcessId,
            info.ProcessName,
            info.WorkingSetMb,
            info.ThreadCount,
            info.HandleCount);

        return new TextResourceContents
        {
            Uri = requestContext.Params.Uri,
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(info, McpResourcesJsonContext.Default.ProcessInfo)
        };
    }

    [McpServerResource(
        UriTemplate = "time://{format}",
        Name = "Current Time",
        MimeType = "text/plain")]
    [Description("Returns the current UTC time in the specified format: 'iso', 'unix', 'rfc', or 'ticks'.")]
    public TextResourceContents GetCurrentTime(
        RequestContext<ReadResourceRequestParams> requestContext,
        string format)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var result = format.ToLowerInvariant() switch
        {
            "iso" => utcNow.ToString("O"),
            "unix" => utcNow.ToUnixTimeSeconds().ToString(),
            "rfc" => utcNow.ToString("R"),
            "ticks" => utcNow.Ticks.ToString(),
            _ => throw new McpException(
                $"Unknown format '{format}'. Supported: iso, unix, rfc, ticks.")
        };

        return new TextResourceContents
        {
            Uri = requestContext.Params.Uri,
            MimeType = "text/plain",
            Text = result
        };
    }
}
