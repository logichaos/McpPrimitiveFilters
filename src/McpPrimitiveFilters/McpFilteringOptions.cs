namespace McpPrimitiveFilters;

public sealed class McpFilteringOptions
{
  public McpFilteringAllowedOptions Allowed { get; set; } = new();
}

public sealed class McpFilteringAllowedOptions
{
  public string[] Tools { get; set; } = [];
  public string[] Resources { get; set; } = [];
  public string[] Prompts { get; set; } = [];
}
