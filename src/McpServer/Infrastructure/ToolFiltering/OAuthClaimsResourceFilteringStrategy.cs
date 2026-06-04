namespace McpServer.Infrastructure.ToolFiltering;

public sealed class OAuthClaimsResourceFilteringStrategy : ResourceFilteringStrategy
{
    public IEnumerable<string> FilterResources(HttpContext httpContext, IEnumerable<string> resourceNames)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return resourceNames;
        }

        var principal = httpContext.User;
        if (principal.HasClaim("scope", "mcp.resources.all"))
            return resourceNames;

        return resourceNames.Where(name =>
        {
            var claimType = $"mcp.resource.{name}";
            return principal.HasClaim("scope", claimType);
        });
    }
}
