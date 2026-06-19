namespace ModelContextProtocol.TestOAuthServer;

public sealed class AuthorizationCodeInfo
{
  public required string ClientId { get; init; }

  public required string RedirectUri { get; init; }

  public required string CodeChallenge { get; init; }

  public List<string> Scope { get; init; } = [];

  public Uri? Resource { get; init; }
}