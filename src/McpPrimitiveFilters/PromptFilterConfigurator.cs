using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpPrimitiveFilters;

internal sealed class PromptFilterConfigurator : McpPrimitiveFilterConfigurator
{
    public PromptFilterConfigurator(
        IEnumerable<McpPrimitiveFilteringStrategy> strategies,
        IOptions<McpPrimitiveFiltersOptions> options,
        ILoggerFactory loggerFactory)
        : base(strategies, options, loggerFactory, options.Value.FilterPrompts, "Prompts") { }

    protected override void RegisterFilters(McpServerOptions o)
    {
        o.Filters.Request.ListPromptsFilters.Add(ListPrompts);
        o.Filters.Request.GetPromptFilters.Add(GetPrompt);
    }

    private McpRequestHandler<ListPromptsRequestParams, ListPromptsResult> ListPrompts(
        McpRequestHandler<ListPromptsRequestParams, ListPromptsResult> next) => async (c, ct) =>
    {
        var r = await next(c, ct);
        if (r.Prompts is { Count: > 0 })
            r.Prompts = FilterByName(McpPrimitiveType.Prompt, "list", r.Prompts, p => p.Name);
        return r;
    };

    private McpRequestHandler<GetPromptRequestParams, GetPromptResult> GetPrompt(
        McpRequestHandler<GetPromptRequestParams, GetPromptResult> next) => async (c, ct) =>
    {
        if (c.Params?.Name is not { } n) return await next(c, ct);
        if (Allows(n, McpPrimitiveType.Prompt, "get")) return await next(c, ct);
        return new GetPromptResult
        {
            Messages = [new PromptMessage
            {
                Role = Role.User,
                Content = new TextContentBlock { Text = $"Prompt '{n}' is not authorized." }
            }]
        };
    };
}
