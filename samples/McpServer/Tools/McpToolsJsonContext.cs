using System.Text.Json.Serialization;

namespace McpServer.Tools;

[JsonSerializable(typeof(DemoUser))]
[JsonSerializable(typeof(List<DemoUser>))]
[JsonSerializable(typeof(ServerStats))]
internal partial class McpToolsJsonContext : JsonSerializerContext { }
