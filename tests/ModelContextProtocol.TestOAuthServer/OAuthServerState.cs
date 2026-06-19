using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.WebUtilities;

namespace ModelContextProtocol.TestOAuthServer;

public sealed class OAuthServerState
{
  private readonly RSA _rsa = RSA.Create(2048);

  public string KeyId { get; } = Guid.NewGuid().ToString();
  public RSA RsaKey => _rsa;

  public string[] ValidResources { get; set; } =
  [
      "http://localhost:5000",
        "http://localhost:5000/mcp",
        "http://localhost:7071",
        "https://localhost:7072"
  ];

  public ConcurrentDictionary<string, AuthorizationCodeInfo> AuthCodes { get; } = new();
  public ConcurrentDictionary<string, TokenInfo> Tokens { get; } = new();
  public ConcurrentDictionary<string, ClientInfo> Clients { get; } = new();
  public ConcurrentQueue<string> MetadataRequests { get; } = new();

  public bool HasRefreshedToken { get; set; }
  public bool ClientIdMetadataDocumentSupported { get; set; } = true;
  public bool ExpectResource { get; set; } = true;
  public bool IncludeOfflineAccessInMetadata { get; set; }
  public HashSet<string> DisabledMetadataPaths { get; } = new(StringComparer.OrdinalIgnoreCase);
  public string? LastRegistrationScope { get; set; }

  public IReadOnlyCollection<string> MetadataRequestsSnapshot => MetadataRequests.ToArray();

  public static string GenerateRandomToken()
  {
    var bytes = new byte[32];
    Random.Shared.NextBytes(bytes);
    return WebEncoders.Base64UrlEncode(bytes);
  }

  public static bool VerifyCodeChallenge(string codeVerifier, string codeChallenge)
  {
    using var sha256 = SHA256.Create();
    var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
    var computedChallenge = WebEncoders.Base64UrlEncode(challengeBytes);
    return computedChallenge == codeChallenge;
  }
}