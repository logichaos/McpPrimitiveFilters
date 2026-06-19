using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpPrimitiveFilters;

internal sealed class ResourceFilterConfigurator : McpPrimitiveFilterConfigurator
{
    public ResourceFilterConfigurator(
        IEnumerable<McpPrimitiveFilteringStrategy> strategies,
        IOptions<McpPrimitiveFiltersOptions> options,
        ILoggerFactory loggerFactory)
        : base(strategies, options, loggerFactory, options.Value.FilterResources, "Resources") { }

    protected override void RegisterFilters(McpServerOptions o)
    {
        o.Filters.Request.ListResourcesFilters.Add(ListResources);
        o.Filters.Request.ListResourceTemplatesFilters.Add(ListResourceTemplates);
        o.Filters.Request.ReadResourceFilters.Add(ReadResource);
    }

    private McpRequestHandler<ListResourcesRequestParams, ListResourcesResult> ListResources(
        McpRequestHandler<ListResourcesRequestParams, ListResourcesResult> next) => async (c, ct) =>
    {
        var r = await next(c, ct);
        if (r.Resources is { Count: > 0 })
            r.Resources = FilterByName(McpPrimitiveType.Resource, "list", r.Resources, x => x.Name);
        return r;
    };

    private McpRequestHandler<ListResourceTemplatesRequestParams, ListResourceTemplatesResult> ListResourceTemplates(
        McpRequestHandler<ListResourceTemplatesRequestParams, ListResourceTemplatesResult> next) => async (c, ct) =>
    {
        var r = await next(c, ct);
        if (r.ResourceTemplates is { Count: > 0 })
            r.ResourceTemplates = FilterByName(
                McpPrimitiveType.Resource, "list", r.ResourceTemplates, x => x.Name);
        return r;
    };

    private McpRequestHandler<ReadResourceRequestParams, ReadResourceResult> ReadResource(
        McpRequestHandler<ReadResourceRequestParams, ReadResourceResult> next) => async (c, ct) =>
    {
        if (c.Params?.Uri is not { } uri) return await next(c, ct);
        var name = ResolveResourceName(c.Services, uri);
        if (name is null || Allows(name, McpPrimitiveType.Resource, "read"))
            return await next(c, ct);
        return new ReadResourceResult
        {
            Contents = [new TextResourceContents
            {
                Uri = uri, MimeType = "text/plain",
                Text = $"Resource '{uri}' is not authorized."
            }]
        };
    };

    private static string? ResolveResourceName(IServiceProvider? services, string uri)
    {
        var serverResources = services?.GetServices<McpServerResource>();
        if (serverResources is null) return null;
        foreach (var resource in serverResources)
            if (resource.IsMatch(uri))
                return resource.ProtocolResource?.Name
                    ?? resource.ProtocolResourceTemplate?.Name;
        return null;
    }
}
