namespace ModelContextProtocol.TestOAuthServer;

public sealed class TokenInfo
{
  public required string ClientId { get; init; }

  public List<string> Scopes { get; init; } = [];

  public required DateTimeOffset IssuedAt { get; init; }

  public required DateTimeOffset ExpiresAt { get; init; }

  public Uri? Resource { get; init; }

  public string? JwtId { get; init; }
}