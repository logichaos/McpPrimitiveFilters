namespace ModelContextProtocol.TestOAuthServer;

public sealed class ClientInfo
{
  public required string ClientId { get; init; }

  public required bool RequiresClientSecret { get; init; }

  public string? ClientSecret { get; init; }

  public List<string> RedirectUris { get; init; } = [];
}