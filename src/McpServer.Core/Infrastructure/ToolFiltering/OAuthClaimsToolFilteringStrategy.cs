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
            ToolFilteringLogMessages.NotAuthenticated(_logger, toolNames.Count());
            return toolNames;
        }

        var principal = httpContext.User;
        var identityName = principal.Identity?.Name ?? "(anonymous)";
        var allScopes = principal.FindAll("scope").Select(c => c.Value).ToList();
        ToolFilteringLogMessages.Scopes(_logger, identityName, allScopes);

        if (principal.HasClaim("scope", "mcp.tools.all"))
        {
            ToolFilteringLogMessages.AllAccess(_logger, identityName);
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
                ToolFilteringLogMessages.Allowed(_logger, identityName, name, scopeValue);
                allowed.Add(name);
            }
            else
            {
                ToolFilteringLogMessages.Denied(_logger, identityName, name, scopeValue);
                denied.Add(name);
            }
        }

        if (denied.Count > 0)
            ToolFilteringLogMessages.Result(_logger, allowed.Count, denied.Count);

        return allowed;
    }
}
