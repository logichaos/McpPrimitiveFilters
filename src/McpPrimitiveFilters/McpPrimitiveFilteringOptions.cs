namespace McpPrimitiveFilters;

public class McpPrimitiveFiltersOptions
{
  public bool UseBuiltinAppSettingsFilteringStrategy { get; set; } = true;
  public bool UseBuiltinOAuthClaimsFilteringStrategy { get; set; } = true;

  public bool FilterTools { get; set; } = true;
  public bool FilterResources { get; set; } = true;
  public bool FilterPrompts { get; set; } = true;
}