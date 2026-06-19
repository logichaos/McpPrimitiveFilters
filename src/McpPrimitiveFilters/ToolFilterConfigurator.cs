using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpPrimitiveFilters;

internal sealed class ToolFilterConfigurator : McpPrimitiveFilterConfigurator
{
    public ToolFilterConfigurator(
        IEnumerable<McpPrimitiveFilteringStrategy> strategies,
        IOptions<McpPrimitiveFiltersOptions> options,
        ILoggerFactory loggerFactory)
        : base(strategies, options, loggerFactory, options.Value.FilterTools, "Tools") { }

    protected override void RegisterFilters(McpServerOptions o)
    {
        o.Filters.Request.ListToolsFilters.Add(ListTools);
        o.Filters.Request.CallToolFilters.Add(CallTool);
    }

    private McpRequestHandler<ListToolsRequestParams, ListToolsResult> ListTools(
        McpRequestHandler<ListToolsRequestParams, ListToolsResult> next) => async (c, ct) =>
    {
        var r = await next(c, ct);
        if (r.Tools is { Count: > 0 })
            r.Tools = FilterByName(McpPrimitiveType.Tool, McpPrimitiveFilterPipeline.OpList, r.Tools, t => t.Name);
        return r;
    };

    private McpRequestHandler<CallToolRequestParams, CallToolResult> CallTool(
        McpRequestHandler<CallToolRequestParams, CallToolResult> next) => async (c, ct) =>
    {
        if (c.Params?.Name is not { } n) return await next(c, ct);
        if (Allows(n, McpPrimitiveType.Tool, McpPrimitiveFilterPipeline.OpCall)) return await next(c, ct);
        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = $"Tool '{n}' is not authorized." }],
            IsError = true
        };
    };
}
