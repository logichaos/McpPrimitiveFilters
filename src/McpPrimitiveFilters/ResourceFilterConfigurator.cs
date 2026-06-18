using McpPrimitiveFilters.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpPrimitiveFilters;

internal sealed class ResourceFilterConfigurator : IConfigureOptions<McpServerOptions>
{
    private readonly McpPrimitiveFilteringStrategy[] _strategies;
    private readonly McpPrimitiveFiltersOptions _options;
    private readonly ILogger _logger;

    public ResourceFilterConfigurator(
        IEnumerable<McpPrimitiveFilteringStrategy> strategies,
        IOptions<McpPrimitiveFiltersOptions> options,
        ILoggerFactory loggerFactory)
    {
        _strategies = [.. strategies];
        _options = options.Value;
        _logger = loggerFactory.CreateLogger($"{nameof(McpPrimitiveFilters)}.Resources");
    }

    public void Configure(McpServerOptions o)
    {
        if (!_options.FilterResources) return;

        o.Filters.Request.ListResourcesFilters.Add(ListResources);
        o.Filters.Request.ListResourceTemplatesFilters.Add(ListResourceTemplates);
        o.Filters.Request.ReadResourceFilters.Add(ReadResource);
    }

    private McpRequestHandler<ListResourcesRequestParams, ListResourcesResult> ListResources(
        McpRequestHandler<ListResourcesRequestParams, ListResourcesResult> next) => async (c, ct) =>
    {
        var r = await next(c, ct);
        if (r.Resources is { Count: > 0 })
            r.Resources = FilterByName(McpPrimitiveType.Resource, r.Resources, x => x.Name);
        return r;
    };

    private McpRequestHandler<ListResourceTemplatesRequestParams, ListResourceTemplatesResult> ListResourceTemplates(
        McpRequestHandler<ListResourceTemplatesRequestParams, ListResourceTemplatesResult> next) => async (c, ct) =>
    {
        var r = await next(c, ct);
        if (r.ResourceTemplates is { Count: > 0 })
            r.ResourceTemplates = FilterByName(McpPrimitiveType.Resource, r.ResourceTemplates, x => x.Name);
        return r;
    };

    private McpRequestHandler<ReadResourceRequestParams, ReadResourceResult> ReadResource(
        McpRequestHandler<ReadResourceRequestParams, ReadResourceResult> next) => async (c, ct) =>
    {
        if (c.Params?.Uri is not { } uri) return await next(c, ct);
        var name = ResolveResourceName(c.Services, uri);
        if (name is null || Allows(name, McpPrimitiveType.Resource)) return await next(c, ct);
        LogDenial(McpPrimitiveType.Resource, name);
        return new ReadResourceResult
        {
            Contents = [new TextResourceContents
            {
                Uri = uri, MimeType = "text/plain",
                Text = $"Resource '{uri}' is not authorized."
            }]
        };
    };

    private List<Resource> FilterByName(McpPrimitiveType type,
        IList<Resource> items, Func<Resource, string> getName)
        => Apply(type, items, getName);

    private List<ResourceTemplate> FilterByName(McpPrimitiveType type,
        IList<ResourceTemplate> items, Func<ResourceTemplate, string> getName)
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
