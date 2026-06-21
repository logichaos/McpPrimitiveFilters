using System.Text.Json.Serialization;

namespace ModelContextProtocol.TestOAuthServer;

internal sealed class OAuthServerMetadata
{
  [JsonPropertyName("issuer")]
  public required string Issuer { get; init; }

  [JsonPropertyName("authorization_endpoint")]
  public required string AuthorizationEndpoint { get; init; }

  [JsonPropertyName("token_endpoint")]
  public required string TokenEndpoint { get; init; }

  [JsonPropertyName("jwks_uri")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? JwksUri { get; init; }

  [JsonPropertyName("registration_endpoint")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? RegistrationEndpoint { get; init; }

  [JsonPropertyName("scopes_supported")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public List<string>? ScopesSupported { get; init; }

  [JsonPropertyName("response_types_supported")]
  public required List<string> ResponseTypesSupported { get; init; }

  [JsonPropertyName("response_modes_supported")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public List<string>? ResponseModesSupported { get; init; }

  [JsonPropertyName("grant_types_supported")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public List<string>? GrantTypesSupported { get; init; }

  [JsonPropertyName("token_endpoint_auth_methods_supported")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public List<string>? TokenEndpointAuthMethodsSupported { get; init; }

  [JsonPropertyName("token_endpoint_auth_signing_alg_values_supported")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public List<string>? TokenEndpointAuthSigningAlgValuesSupported { get; init; }

  [JsonPropertyName("introspection_endpoint")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? IntrospectionEndpoint { get; init; }

  [JsonPropertyName("introspection_endpoint_auth_methods_supported")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public List<string>? IntrospectionEndpointAuthMethodsSupported { get; init; }

  [JsonPropertyName("introspection_endpoint_auth_signing_alg_values_supported")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public List<string>? IntrospectionEndpointAuthSigningAlgValuesSupported { get; init; }

  [JsonPropertyName("revocation_endpoint")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? RevocationEndpoint { get; init; }

  [JsonPropertyName("revocation_endpoint_auth_methods_supported")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public List<string>? RevocationEndpointAuthMethodsSupported { get; init; }

  [JsonPropertyName("revocation_endpoint_auth_signing_alg_values_supported")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public List<string>? RevocationEndpointAuthSigningAlgValuesSupported { get; init; }

  [JsonPropertyName("code_challenge_methods_supported")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public List<string>? CodeChallengeMethodsSupported { get; init; }

  [JsonPropertyName("subject_types_supported")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public List<string>? SubjectTypesSupported { get; init; }

  [JsonPropertyName("id_token_signing_alg_values_supported")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public List<string>? IdTokenSigningAlgValuesSupported { get; init; }

  [JsonPropertyName("claims_supported")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public List<string>? ClaimsSupported { get; init; }

  [JsonPropertyName("client_id_metadata_document_supported")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? ClientIdMetadataDocumentSupported { get; init; }
}