using System.Text.Json.Serialization;

namespace ModelContextProtocol.TestOAuthServer;

internal sealed class AuthorizationServerMetadata
{
  [JsonPropertyName("issuer")]
  public required Uri Issuer { get; init; }

  [JsonPropertyName("authorization_endpoint")]
  public required Uri AuthorizationEndpoint { get; init; }

  [JsonPropertyName("token_endpoint")]
  public required Uri TokenEndpoint { get; init; }

  [JsonPropertyName("introspection_endpoint")]
  public Uri? IntrospectionEndpoint => new($"{Issuer}/introspect");

  [JsonPropertyName("response_types_supported")]
  public required List<string> ResponseTypesSupported { get; init; }

  [JsonPropertyName("grant_types_supported")]
  public required List<string> GrantTypesSupported { get; init; }

  [JsonPropertyName("token_endpoint_auth_methods_supported")]
  public required List<string> TokenEndpointAuthMethodsSupported { get; init; }

  [JsonPropertyName("code_challenge_methods_supported")]
  public required List<string> CodeChallengeMethodsSupported { get; init; }

  [JsonPropertyName("scopes_supported")]
  public List<string>? ScopesSupported { get; init; }

  [JsonPropertyName("client_id_metadata_document_supported")]
  public bool ClientIdMetadataDocumentSupported { get; init; }
}