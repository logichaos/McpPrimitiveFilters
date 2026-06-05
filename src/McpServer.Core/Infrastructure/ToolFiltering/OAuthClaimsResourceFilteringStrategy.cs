namespace McpServer.Infrastructure.ToolFiltering;

public sealed class OAuthClaimsResourceFilteringStrategy : ResourceFilteringStrategy
{
    private readonly ILogger<OAuthClaimsResourceFilteringStrategy> _logger;

    public OAuthClaimsResourceFilteringStrategy(ILogger<OAuthClaimsResourceFilteringStrategy> logger)
    {
        _logger = logger;
    }

    public IEnumerable<string> FilterResources(HttpContext httpContext, IEnumerable<string> resourceNames)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            ResourceFilteringLogMessages.NotAuthenticated(_logger, resourceNames.Count());
            return resourceNames;
        }

        var principal = httpContext.User;
        var identityName = principal.Identity?.Name ?? "(anonymous)";
        var allScopes = principal.FindAll("scope").Select(c => c.Value).ToList();
        ResourceFilteringLogMessages.Scopes(_logger, identityName, allScopes);

        if (principal.HasClaim("scope", "mcp.resources.all"))
        {
            ResourceFilteringLogMessages.AllAccess(_logger, identityName);
            return resourceNames;
        }

        var resourceArray = resourceNames.ToArray();
        var allowed = new List<string>();
        var denied = new List<string>();

        foreach (var name in resourceArray)
        {
            var scopeValue = $"mcp.resource.{name}";
            if (principal.HasClaim("scope", scopeValue))
            {
                ResourceFilteringLogMessages.Allowed(_logger, identityName, name, scopeValue);
                allowed.Add(name);
            }
            else
            {
                ResourceFilteringLogMessages.Denied(_logger, identityName, name, scopeValue);
                denied.Add(name);
            }
        }

        if (denied.Count > 0)
            ResourceFilteringLogMessages.Result(_logger, allowed.Count, denied.Count);

        return allowed;
    }
}
