using System.Security.Claims;
using McpAuthorizationFiltering.Logging;

namespace McpAuthorizationFiltering.Strategies;

public sealed class OAuthClaimsFilteringStrategy : McpPrimitiveFilteringStrategy
{
    private readonly ILogger<OAuthClaimsFilteringStrategy> _logger;

    public OAuthClaimsFilteringStrategy(ILogger<OAuthClaimsFilteringStrategy> logger)
        => _logger = logger;

    protected override IEnumerable<string> FilterTools(HttpContext ctx, IEnumerable<string> names)
        => FilterByScope(ctx, McpPrimitiveType.Tool, names, "mcp.tool.", "mcp.tools.all");

    protected override IEnumerable<string> FilterResources(HttpContext ctx, IEnumerable<string> names)
        => FilterByScope(ctx, McpPrimitiveType.Resource, names, "mcp.resource.", "mcp.resources.all");

    protected override IEnumerable<string> FilterPrompts(HttpContext ctx, IEnumerable<string> names)
        => FilterByScope(ctx, McpPrimitiveType.Prompt, names, "mcp.prompt.", "mcp.prompts.all");

    private IEnumerable<string> FilterByScope(
        HttpContext ctx, McpPrimitiveType type,
        IEnumerable<string> names, string scopePrefix, string allScope)
    {
        if (ctx.User.Identity?.IsAuthenticated != true)
        {
            McpFilteringLogMessages.NotAuthenticated(_logger, type, names.Count());
            return names;
        }

        var principal = ctx.User;
        var identityName = principal.Identity?.Name ?? "(anonymous)";

        if (principal.HasClaim("scope", allScope))
        {
            McpFilteringLogMessages.AllAccess(_logger, type, identityName);
            return names;
        }

        var nameArray = names.ToArray();
        var allowed = new List<string>();
        var denied = new List<string>();

        foreach (var name in nameArray)
        {
            if (principal.HasClaim("scope", scopePrefix + name))
            {
                McpFilteringLogMessages.Allowed(_logger, type, identityName, name);
                allowed.Add(name);
            }
            else
            {
                McpFilteringLogMessages.Denied(_logger, type, identityName, name);
                denied.Add(name);
            }
        }

        if (denied.Count > 0)
            McpFilteringLogMessages.Result(_logger, type, allowed.Count, denied.Count);

        return allowed;
    }
}
