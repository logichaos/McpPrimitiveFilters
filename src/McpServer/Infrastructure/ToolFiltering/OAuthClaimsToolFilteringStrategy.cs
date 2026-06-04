namespace McpServer.Infrastructure.ToolFiltering;

public sealed class OAuthClaimsToolFilteringStrategy : ToolFilteringStrategy
{
    private readonly ILogger<OAuthClaimsToolFilteringStrategy> _logger;

    public OAuthClaimsToolFilteringStrategy(ILogger<OAuthClaimsToolFilteringStrategy> logger)
    {
        _logger = logger;
    }

    public IEnumerable<string> FilterTools(HttpContext httpContext, IEnumerable<string> toolNames)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug("Tool filtering: user not authenticated — allowing all {Count} tools", toolNames.Count());
            return toolNames;
        }

        var principal = httpContext.User;
        var identityName = principal.Identity?.Name ?? "(anonymous)";
        var allScopes = principal.FindAll("scope").Select(c => c.Value).ToList();
        _logger.LogDebug("Tool filtering: user={User}, scopes={Scopes}", identityName, allScopes);

        if (principal.HasClaim("scope", "mcp.tools.all"))
        {
            _logger.LogInformation("Tool filtering: user={User} has mcp.tools.all scope — allowing all tools", identityName);
            return toolNames;
        }

        var toolArray = toolNames.ToArray();
        var allowed = new List<string>();
        var denied = new List<string>();

        foreach (var name in toolArray)
        {
            var scopeValue = $"mcp.tool.{name}";
            if (principal.HasClaim("scope", scopeValue))
            {
                _logger.LogDebug("Tool filtering: user={User} allowed tool '{Tool}' via scope '{Scope}'", identityName, name, scopeValue);
                allowed.Add(name);
            }
            else
            {
                _logger.LogDebug("Tool filtering: user={User} denied tool '{Tool}' — missing scope '{Scope}'", identityName, name, scopeValue);
                denied.Add(name);
            }
        }

        if (denied.Count > 0)
            _logger.LogInformation("Tool filtering result: {Allowed} allowed, {Denied} denied", allowed.Count, denied.Count);

        return allowed;
    }
}
