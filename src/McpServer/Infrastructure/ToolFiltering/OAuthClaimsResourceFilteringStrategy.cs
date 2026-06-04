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
            _logger.LogDebug("Resource filtering: user not authenticated — allowing all {Count} resources", resourceNames.Count());
            return resourceNames;
        }

        var principal = httpContext.User;
        var identityName = principal.Identity?.Name ?? "(anonymous)";
        var allScopes = principal.FindAll("scope").Select(c => c.Value).ToList();
        _logger.LogDebug("Resource filtering: user={User}, scopes={Scopes}", identityName, allScopes);

        if (principal.HasClaim("scope", "mcp.resources.all"))
        {
            _logger.LogInformation("Resource filtering: user={User} has mcp.resources.all scope — allowing all resources", identityName);
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
                _logger.LogDebug("Resource filtering: user={User} allowed resource '{Resource}' via scope '{Scope}'", identityName, name, scopeValue);
                allowed.Add(name);
            }
            else
            {
                _logger.LogDebug("Resource filtering: user={User} denied resource '{Resource}' — missing scope '{Scope}'", identityName, name, scopeValue);
                denied.Add(name);
            }
        }

        if (denied.Count > 0)
            _logger.LogInformation("Resource filtering result: {Allowed} allowed, {Denied} denied", allowed.Count, denied.Count);

        return allowed;
    }
}
