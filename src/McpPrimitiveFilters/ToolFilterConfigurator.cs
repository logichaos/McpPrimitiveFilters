using McpPrimitiveFilters.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpPrimitiveFilters;

internal sealed class ToolFilterConfigurator : IConfigureOptions<McpServerOptions>
{
    private readonly McpPrimitiveFilteringStrategy[] _strategies;
    private readonly McpPrimitiveFiltersOptions _options;
    private readonly ILogger _logger;

    public ToolFilterConfigurator(
        IEnumerable<McpPrimitiveFilteringStrategy> strategies,
        IOptions<McpPrimitiveFiltersOptions> options,
        ILoggerFactory loggerFactory)
    {
        _strategies = [.. strategies];
        _options = options.Value;
        _logger = loggerFactory.CreateLogger($"{nameof(McpPrimitiveFilters)}.Tools");
    }

    public void Configure(McpServerOptions o)
    {
        if (!_options.FilterTools) return;

        o.Filters.Request.ListToolsFilters.Add(ListTools);
        o.Filters.Request.CallToolFilters.Add(CallTool);
    }

    private McpRequestHandler<ListToolsRequestParams, ListToolsResult> ListTools(
        McpRequestHandler<ListToolsRequestParams, ListToolsResult> next) => async (c, ct) =>
    {
        var r = await next(c, ct);
        if (r.Tools is { Count: > 0 })
            r.Tools = FilterByName(McpPrimitiveType.Tool, r.Tools, t => t.Name);
        return r;
    };

    private McpRequestHandler<CallToolRequestParams, CallToolResult> CallTool(
        McpRequestHandler<CallToolRequestParams, CallToolResult> next) => async (c, ct) =>
    {
        if (c.Params?.Name is not { } n) return await next(c, ct);
        if (Allows(n, McpPrimitiveType.Tool)) return await next(c, ct);
        LogDenial(McpPrimitiveType.Tool, n);
        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = $"Tool '{n}' is not authorized." }],
            IsError = true
        };
    };

    private List<Tool> FilterByName(McpPrimitiveType type,
        IList<Tool> items, Func<Tool, string> getName)
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
