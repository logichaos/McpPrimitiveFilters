using System.Text.Json.Serialization;

namespace McpServer.Resources;

[JsonSerializable(typeof(SystemInfo))]
[JsonSerializable(typeof(WeatherInfo))]
[JsonSerializable(typeof(ProcessInfo))]
internal partial class McpResourcesJsonContext : JsonSerializerContext;