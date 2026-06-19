using System.Text.Json.Serialization;

namespace ModelContextProtocol.TestOAuthServer;

internal sealed class TokenIntrospectionResponse
{
  [JsonPropertyName("active")]
  public required bool Active { get; init; }

  [JsonPropertyName("client_id")]
  public string? ClientId { get; init; }

  [JsonPropertyName("scope")]
  public string? Scope { get; init; }

  [JsonPropertyName("exp")]
  public long? ExpirationTime { get; init; }

  [JsonPropertyName("aud")]
  public string? Audience { get; init; }
}