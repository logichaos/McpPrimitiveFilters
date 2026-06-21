using System.Text.Json.Serialization;

namespace ModelContextProtocol.TestOAuthServer;

internal sealed class ClientRegistrationRequest
{
  [JsonPropertyName("redirect_uris")]
  public required List<string> RedirectUris { get; init; }

  [JsonPropertyName("token_endpoint_auth_method")]
  public string? TokenEndpointAuthMethod { get; init; }

  [JsonPropertyName("grant_types")]
  public List<string>? GrantTypes { get; init; }

  [JsonPropertyName("response_types")]
  public List<string>? ResponseTypes { get; init; }

  [JsonPropertyName("client_name")]
  public string? ClientName { get; init; }

  [JsonPropertyName("client_uri")]
  public string? ClientUri { get; init; }

  [JsonPropertyName("logo_uri")]
  public string? LogoUri { get; init; }

  [JsonPropertyName("scope")]
  public string? Scope { get; init; }

  [JsonPropertyName("contacts")]
  public List<string>? Contacts { get; init; }

  [JsonPropertyName("tos_uri")]
  public string? TosUri { get; init; }

  [JsonPropertyName("policy_uri")]
  public string? PolicyUri { get; init; }

  [JsonPropertyName("jwks_uri")]
  public string? JwksUri { get; init; }

  [JsonPropertyName("software_id")]
  public string? SoftwareId { get; init; }

  [JsonPropertyName("software_version")]
  public string? SoftwareVersion { get; init; }
}