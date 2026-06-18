using McpPrimitiveFilters.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpPrimitiveFilters;

internal sealed class PromptFilterConfigurator : IConfigureOptions<McpServerOptions>
{
    private readonly McpPrimitiveFilteringStrategy[] _strategies;
    private readonly McpPrimitiveFiltersOptions _options;
    private readonly ILogger _logger;

    public PromptFilterConfigurator(
        IEnumerable<McpPrimitiveFilteringStrategy> strategies,
        IOptions<McpPrimitiveFiltersOptions> options,
        ILoggerFactory loggerFactory)
    {
        _strategies = [.. strategies];
        _options = options.Value;
        _logger = loggerFactory.CreateLogger($"{nameof(McpPrimitiveFilters)}.Prompts");
    }

    public void Configure(McpServerOptions o)
    {
        if (!_options.FilterPrompts) return;

        o.Filters.Request.ListPromptsFilters.Add(ListPrompts);
        o.Filters.Request.GetPromptFilters.Add(GetPrompt);
    }

    private McpRequestHandler<ListPromptsRequestParams, ListPromptsResult> ListPrompts(
        McpRequestHandler<ListPromptsRequestParams, ListPromptsResult> next) => async (c, ct) =>
    {
        var r = await next(c, ct);
        if (r.Prompts is { Count: > 0 })
            r.Prompts = FilterByName(McpPrimitiveType.Prompt, r.Prompts, p => p.Name);
        return r;
    };

    private McpRequestHandler<GetPromptRequestParams, GetPromptResult> GetPrompt(
        McpRequestHandler<GetPromptRequestParams, GetPromptResult> next) => async (c, ct) =>
    {
        if (c.Params?.Name is not { } n) return await next(c, ct);
        if (Allows(n, McpPrimitiveType.Prompt)) return await next(c, ct);
        LogDenial(McpPrimitiveType.Prompt, n);
        return new GetPromptResult
        {
            Messages = [new PromptMessage
            {
                Role = Role.User,
                Content = new TextContentBlock { Text = $"Prompt '{n}' is not authorized." }
            }]
        };
    };

    private List<Prompt> FilterByName(McpPrimitiveType type,
        IList<Prompt> items, Func<Prompt, string> getName)
        => Apply(type, items, getName);

    private List<T> Apply<T>(McpPrimitiveType type,
        IList<T> items, Func<T, string> getName)
    {
        if (_strategies.Length == 0) return [.. items];

        var names = items.Select(getName).ToList();
        foreach (var s in _strategies)
            names = [.. s.FilterPrimitives(type, names)];

        var allowed = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        return [.. items.Where(item => allowed.Contains(getName(item)))];
    }

    private bool Allows(string name, McpPrimitiveType type)
    {
        if (_strategies.Length == 0) return true;
        var names = new[] { name }.AsEnumerable();
        foreach (var s in _strategies)
            names = s.FilterPrimitives(type, names);
        return names.Any();
    }

    private void LogDenial(McpPrimitiveType type, string name)
        => McpFilteringLogMessages.CallDenied(_logger, type, null, name);
}
