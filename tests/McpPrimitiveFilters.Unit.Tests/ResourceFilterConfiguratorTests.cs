using FakeItEasy;
using McpPrimitiveFilters;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpPrimitiveFilters.Unit.Tests;

public class ResourceFilterConfiguratorTests
{
    private static ResourceFilterConfigurator Create(
        IEnumerable<McpPrimitiveFilteringStrategy>? strategies = null,
        McpPrimitiveFiltersOptions? options = null)
    {
        return new ResourceFilterConfigurator(
            strategies ?? [],
            Options.Create(options ?? new McpPrimitiveFiltersOptions()),
            NullLoggerFactory.Instance);
    }

    private static RequestContext<TParams> CreateContext<TParams>(TParams parameters)
    {
        return new RequestContext<TParams>(
            A.Fake<McpServer>(), new JsonRpcRequest { Method = "test" }, parameters);
    }

    [Test]
    public async Task Configure_FilterResourcesEnabled_AddsResourceFilters()
    {
        var o = new McpServerOptions();
        Create().Configure(o);

        await Assert.That(o.Filters.Request.ListResourcesFilters).Count().IsEqualTo(1);
        await Assert.That(o.Filters.Request.ListResourceTemplatesFilters).Count().IsEqualTo(1);
        await Assert.That(o.Filters.Request.ReadResourceFilters).Count().IsEqualTo(1);
        await Assert.That(o.Filters.Request.ListToolsFilters).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Configure_FilterResourcesDisabled_AddsNoFilters()
    {
        var o = new McpServerOptions();
        Create(options: new McpPrimitiveFiltersOptions { FilterResources = false }).Configure(o);

        await Assert.That(o.Filters.Request.ListResourcesFilters).Count().IsEqualTo(0);
        await Assert.That(o.Filters.Request.ReadResourceFilters).Count().IsEqualTo(0);
    }

    [Test]
    public async Task ListResourcesFilter_NoStrategies_ReturnsAll()
    {
        var o = new McpServerOptions();
        Create().Configure(o);
        var filter = o.Filters.Request.ListResourcesFilters[0];

        var next = ResHandler(new ListResourcesResult
        {
            Resources = [new Resource { Name = "a", Uri = "x://a" }, new Resource { Name = "b", Uri = "x://b" }]
        });
        var result = await filter(next)(CreateContext(new ListResourcesRequestParams()), CancellationToken.None);

        await Assert.That(result.Resources).Count().IsEqualTo(2);
    }

    [Test]
    public async Task ListResourcesFilter_WithBlockingStrategy_Filters()
    {
        var o = new McpServerOptions();
        Create([new AllowOnly("a")]).Configure(o);
        var filter = o.Filters.Request.ListResourcesFilters[0];

        var next = ResHandler(new ListResourcesResult
        {
            Resources = [new Resource { Name = "a", Uri = "x://a" }, new Resource { Name = "b", Uri = "x://b" }]
        });
        var result = await filter(next)(CreateContext(new ListResourcesRequestParams()), CancellationToken.None);

        await Assert.That(result.Resources).Count().IsEqualTo(1);
        await Assert.That(result.Resources![0].Name).IsEqualTo("a");
    }

    [Test]
    public async Task ReadResourceFilter_NoUri_PassesThrough()
    {
        var o = new McpServerOptions();
        Create([new DenyAll()]).Configure(o);
        var filter = o.Filters.Request.ReadResourceFilters[0];

        var next = ReadHandler(new ReadResourceResult());
        var result = await filter(next)(CreateContext(new ReadResourceRequestParams { Uri = null! }), CancellationToken.None);

        await Assert.That(result.Contents).IsEmpty();
    }

    private static McpRequestHandler<ListResourcesRequestParams, ListResourcesResult> ResHandler(ListResourcesResult r)
        => (_, _) => ValueTask.FromResult(r);

    private static McpRequestHandler<ReadResourceRequestParams, ReadResourceResult> ReadHandler(ReadResourceResult r)
        => (_, _) => ValueTask.FromResult(r);

    private sealed class AllowOnly(params string[] allowed) : McpPrimitiveFilteringStrategy
    {
        protected override IEnumerable<string> FilterResources(IEnumerable<string> n) => n.Where(allowed.Contains);
    }

    private sealed class DenyAll : McpPrimitiveFilteringStrategy
    {
        protected override IEnumerable<string> FilterResources(IEnumerable<string> n) => [];
    }
}
