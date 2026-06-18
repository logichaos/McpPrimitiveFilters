namespace McpAuthorizationFiltering;

public abstract class McpPrimitiveFilteringStrategy
{
    public IEnumerable<string> FilterPrimitives(
        HttpContext httpContext,
        McpPrimitiveType type,
        IEnumerable<string> names)
    {
        return type switch
        {
            McpPrimitiveType.Tool     => FilterTools(httpContext, names),
            McpPrimitiveType.Resource => FilterResources(httpContext, names),
            McpPrimitiveType.Prompt   => FilterPrompts(httpContext, names),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    protected virtual IEnumerable<string> FilterTools(HttpContext httpContext, IEnumerable<string> names) => names;
    protected virtual IEnumerable<string> FilterResources(HttpContext httpContext, IEnumerable<string> names) => names;
    protected virtual IEnumerable<string> FilterPrompts(HttpContext httpContext, IEnumerable<string> names) => names;
}
