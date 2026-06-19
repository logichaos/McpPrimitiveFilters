using System.Text.Json.Serialization;

namespace ModelContextProtocol.TestOAuthServer;

internal sealed class JsonWebKeySet
{
  [JsonPropertyName("keys")]
  public required JsonWebKey[] Keys { get; init; }
}