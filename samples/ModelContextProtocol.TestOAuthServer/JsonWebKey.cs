using System.Text.Json.Serialization;

namespace ModelContextProtocol.TestOAuthServer;

internal sealed class JsonWebKey
{
  [JsonPropertyName("kty")]
  public required string KeyType { get; init; }

  [JsonPropertyName("use")]
  public required string Use { get; init; }

  [JsonPropertyName("kid")]
  public required string KeyId { get; init; }

  [JsonPropertyName("alg")]
  public required string Algorithm { get; init; }

  [JsonPropertyName("e")]
  public required string Exponent { get; init; }

  [JsonPropertyName("n")]
  public required string Modulus { get; init; }
}