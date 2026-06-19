using System.Text.Json.Serialization;

namespace ModelContextProtocol.TestOAuthServer;

internal sealed class ClientRegistrationResponse
{
  [JsonPropertyName("client_id")]
  public required string ClientId { get; init; }

  [JsonPropertyName("client_secret")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? ClientSecret { get; init; }

  [JsonPropertyName("redirect_uris")]
  public required List<string> RedirectUris { get; init; }

  [JsonPropertyName("registration_access_token")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? RegistrationAccessToken { get; init; }

  [JsonPropertyName("registration_client_uri")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? RegistrationClientUri { get; init; }

  [JsonPropertyName("client_id_issued_at")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public long? ClientIdIssuedAt { get; init; }

  [JsonPropertyName("client_secret_expires_at")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public long? ClientSecretExpiresAt { get; init; }

  [JsonPropertyName("token_endpoint_auth_method")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? TokenEndpointAuthMethod { get; init; }

  [JsonPropertyName("grant_types")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public List<string>? GrantTypes { get; init; }

  [JsonPropertyName("response_types")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public List<string>? ResponseTypes { get; init; }

  [JsonPropertyName("client_name")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? ClientName { get; init; }

  [JsonPropertyName("client_uri")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? ClientUri { get; init; }

  [JsonPropertyName("logo_uri")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? LogoUri { get; init; }

  [JsonPropertyName("scope")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Scope { get; init; }

  [JsonPropertyName("contacts")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public List<string>? Contacts { get; init; }

  [JsonPropertyName("tos_uri")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? TosUri { get; init; }

  [JsonPropertyName("policy_uri")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? PolicyUri { get; init; }

  [JsonPropertyName("jwks_uri")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? JwksUri { get; init; }

  [JsonPropertyName("software_id")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? SoftwareId { get; init; }

  [JsonPropertyName("software_version")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? SoftwareVersion { get; init; }
}