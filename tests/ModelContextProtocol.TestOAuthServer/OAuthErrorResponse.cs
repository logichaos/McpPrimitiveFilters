using System.Text.Json.Serialization;

namespace ModelContextProtocol.TestOAuthServer;

internal sealed class OAuthErrorResponse
{
  [JsonPropertyName("error")]
  public required string Error { get; init; }

  [JsonPropertyName("error_description")]
  public required string ErrorDescription { get; init; }
}