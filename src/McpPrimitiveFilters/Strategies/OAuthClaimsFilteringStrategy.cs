using Microsoft.AspNetCore.Http;
using McpPrimitiveFilters.Logging;

namespace McpPrimitiveFilters.Strategies;

public sealed class OAuthClaimsFilteringStrategy : McpPrimitiveFilteringStrategy
{
    private readonly IHttpContextAccessor _httpAccessor;
    private readonly ILogger<OAuthClaimsFilteringStrategy> _logger;

    public OAuthClaimsFilteringStrategy(
        IHttpContextAccessor httpAccessor,
        ILogger<OAuthClaimsFilteringStrategy> logger)
    {
        _httpAccessor = httpAccessor;
        _logger = logger;
    }

    protected override IEnumerable<string> FilterTools(IEnumerable<string> names)
        => FilterByScope(McpPrimitiveType.Tool, names, "mcp.tool.", "mcp.tools.all");

    protected override IEnumerable<string> FilterResources(IEnumerable<string> names)
        => FilterByScope(McpPrimitiveType.Resource, names, "mcp.resource.", "mcp.resources.all");

    protected override IEnumerable<string> FilterPrompts(IEnumerable<string> names)
        => FilterByScope(McpPrimitiveType.Prompt, names, "mcp.prompt.", "mcp.prompts.all");

    private IEnumerable<string> FilterByScope(
        McpPrimitiveType type,
        IEnumerable<string> names, string scopePrefix, string allScope)
    {
        var nameArray = names.ToArray();

        var ctx = _httpAccessor.HttpContext;
        if (ctx?.User.Identity?.IsAuthenticated != true)
        {
            McpFilteringLogMessages.NotAuthenticated(_logger, type, nameArray.Length);
            return nameArray;
        }

        var principal = ctx.User;
        var identityName = principal.Identity?.Name ?? "(anonymous)";

        if (principal.HasClaim("scope", allScope))
        {
            McpFilteringLogMessages.AllAccess(_logger, type, identityName);
            return nameArray;
        }

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
