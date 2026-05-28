namespace McpServer.Infrastructure.OAuth;

public sealed class OAuthOptions
{
    public const string SectionName = "Mcp:OAuth";

    public string DefaultScheme { get; set; } = string.Empty;

    public string ServerUrl { get; set; } = string.Empty;

    public string[] ScopesSupported { get; set; } = [];

    public string? ResourceDocumentation { get; set; }

    public Dictionary<string, OAuthSchemeConfig> Schemes { get; set; } = new();
}

public sealed class OAuthSchemeConfig
{
    public bool Enabled { get; set; }

    public string? DisplayName { get; set; }

    public string Type { get; set; } = string.Empty;

    public string? AuthorityUrl { get; set; }

    public string? Audience { get; set; }

    public string? Issuer { get; set; }

    public bool DisableBackchannelSslValidation { get; set; }

    public string? TenantId { get; set; }

    public string? Instance { get; set; }

    public string? Domain { get; set; }

    public string? ClientId { get; set; }
}
