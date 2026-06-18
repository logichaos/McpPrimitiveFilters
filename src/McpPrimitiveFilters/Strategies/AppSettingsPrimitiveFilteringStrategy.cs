using Microsoft.Extensions.Configuration;

namespace McpPrimitiveFilters.Strategies;

public sealed class AppSettingsPrimitiveFilteringStrategy : McpPrimitiveFilteringStrategy
{
    private static readonly string[] Empty = [];

    private readonly IConfiguration _configuration;

    public AppSettingsPrimitiveFilteringStrategy(IConfiguration configuration)
        => _configuration = configuration;

    protected override IEnumerable<string> FilterTools(HttpContext httpContext, IEnumerable<string> names)
        => ApplyAllowlist("McpFiltering:Allowed:tools", names);

    protected override IEnumerable<string> FilterResources(HttpContext httpContext, IEnumerable<string> names)
        => ApplyAllowlist("McpFiltering:Allowed:resources", names);

    protected override IEnumerable<string> FilterPrompts(HttpContext httpContext, IEnumerable<string> names)
        => ApplyAllowlist("McpFiltering:Allowed:prompts", names);

    private IEnumerable<string> ApplyAllowlist(string configKey, IEnumerable<string> names)
    {
        var allowed = _configuration.GetSection(configKey).Get<string[]>() ?? Empty;

        if (allowed.Length == 0)
            return names;

        var allowedSet = new HashSet<string>(allowed, StringComparer.OrdinalIgnoreCase);
        return names.Where(allowedSet.Contains);
    }
}
