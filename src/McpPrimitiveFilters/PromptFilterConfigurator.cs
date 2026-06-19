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
        => McpPrimitiveFilterPipeline.Apply(type, "list", items, getName, _strategies, _logger);

    private bool Allows(string name, McpPrimitiveType type)
        => McpPrimitiveFilterPipeline.Allows(name, type, "get", _strategies, _logger);
}
