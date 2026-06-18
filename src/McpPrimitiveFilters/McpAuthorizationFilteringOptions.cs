namespace McpAuthorizationFiltering;

public class McpAuthorizationFilteringOptions
{
    public bool AppSettingsEnabled { get; set; } = true;
    public bool OAuthClaimsEnabled { get; set; } = true;

    public bool FilterTools { get; set; } = true;
    public bool FilterResources { get; set; } = true;
    public bool FilterPrompts { get; set; } = false;
}
