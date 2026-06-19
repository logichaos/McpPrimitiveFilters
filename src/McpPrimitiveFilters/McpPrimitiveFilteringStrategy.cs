namespace McpPrimitiveFilters;

public abstract class McpPrimitiveFilteringStrategy
{
  public IEnumerable<string> FilterPrimitives(
      McpPrimitiveType type,
      IEnumerable<string> names)
  {
    return type switch
    {
      McpPrimitiveType.Tool => FilterTools(names),
      McpPrimitiveType.Resource => FilterResources(names),
      McpPrimitiveType.Prompt => FilterPrompts(names),
      _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
  }

  protected virtual IEnumerable<string> FilterTools(IEnumerable<string> names) => names;
  protected virtual IEnumerable<string> FilterResources(IEnumerable<string> names) => names;
  protected virtual IEnumerable<string> FilterPrompts(IEnumerable<string> names) => names;
}