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
        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = $"Tool '{n}' is not authorized." }],
            IsError = true
        };
    };

    private List<Tool> FilterByName(McpPrimitiveType type,
        IList<Tool> items, Func<Tool, string> getName)
        => McpPrimitiveFilterPipeline.Apply(type, "list", items, getName, _strategies, _logger);

    private bool Allows(string name, McpPrimitiveType type)
        => McpPrimitiveFilterPipeline.Allows(name, type, "call", _strategies, _logger);
}
