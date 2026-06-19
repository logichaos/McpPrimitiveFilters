namespace ModelContextProtocol.TestOAuthServer;

/// <summary>
/// Represents client information for OAuth flow.
/// </summary>
public sealed class ClientInfo
{
  /// <summary>
  /// Gets or sets the client ID.
  /// </summary>
  public required string ClientId { get; init; }

  /// <summary>
  /// Gets or sets whether a client secret is required.
  /// </summary>
  public required bool RequiresClientSecret { get; init; }

  /// <summary>
  /// Gets or sets the client secret.
  /// </summary>
  public string? ClientSecret { get; init; }

  /// <summary>
  /// Gets or sets the list of redirect URIs allowed for this client.
  /// </summary>
  public List<string> RedirectUris { get; init; } = [];
}