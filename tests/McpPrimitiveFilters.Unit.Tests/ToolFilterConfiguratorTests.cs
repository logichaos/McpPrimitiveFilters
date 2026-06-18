using FakeItEasy;
using McpPrimitiveFilters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpPrimitiveFilters.Unit.Tests;

public class ToolFilterConfiguratorTests
{
    private static ToolFilterConfigurator Create(
        IEnumerable<McpPrimitiveFilteringStrategy>? strategies = null,
        McpPrimitiveFiltersOptions? options = null)
    {
        return new ToolFilterConfigurator(
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
    public async Task Configure_FilterToolsEnabled_AddsToolFilters()
    {
        var o = new McpServerOptions();
        Create().Configure(o);

        await Assert.That(o.Filters.Request.ListToolsFilters).Count().IsEqualTo(1);
        await Assert.That(o.Filters.Request.CallToolFilters).Count().IsEqualTo(1);
        await Assert.That(o.Filters.Request.ListResourcesFilters).Count().IsEqualTo(0);
    }

    [Test]
    public async Task Configure_FilterToolsDisabled_AddsNoFilters()
    {
        var o = new McpServerOptions();
        Create(options: new McpPrimitiveFiltersOptions { FilterTools = false }).Configure(o);

        await Assert.That(o.Filters.Request.ListToolsFilters).Count().IsEqualTo(0);
        await Assert.That(o.Filters.Request.CallToolFilters).Count().IsEqualTo(0);
    }

    [Test]
    public async Task ListToolsFilter_NoStrategies_ReturnsAll()
    {
        var o = new McpServerOptions();
        Create().Configure(o);
        var filter = o.Filters.Request.ListToolsFilters[0];

        var next = ToolsHandler(new ListToolsResult
        {
            Tools = [new Tool { Name = "a" }, new Tool { Name = "b" }]
        });
        var result = await filter(next)(CreateContext(new ListToolsRequestParams()), CancellationToken.None);

        await Assert.That(result.Tools).Count().IsEqualTo(2);
    }

    [Test]
    public async Task ListToolsFilter_WithBlockingStrategy_FiltersTools()
    {
        var o = new McpServerOptions();
        Create([new AllowOnly("a")]).Configure(o);
        var filter = o.Filters.Request.ListToolsFilters[0];

        var next = ToolsHandler(new ListToolsResult
        {
            Tools = [new Tool { Name = "a" }, new Tool { Name = "b" }]
        });
        var result = await filter(next)(CreateContext(new ListToolsRequestParams()), CancellationToken.None);

        await Assert.That(result.Tools).Count().IsEqualTo(1);
        await Assert.That(result.Tools![0].Name).IsEqualTo("a");
    }

    [Test]
    public async Task CallToolFilter_Denied_ReturnsError()
    {
        var o = new McpServerOptions();
        Create([new DenyAll()]).Configure(o);
        var filter = o.Filters.Request.CallToolFilters[0];

        var next = CallHandler(new CallToolResult());
        var result = await filter(next)(CreateContext(new CallToolRequestParams { Name = "x" }), CancellationToken.None);

        await Assert.That(result.IsError).IsTrue();
        var text = ((TextContentBlock)result.Content![0]).Text;
        await Assert.That(text).Contains("not authorized");
    }

    [Test]
    public async Task CallToolFilter_Allowed_DelegatesToNext()
    {
        var o = new McpServerOptions();
        Create([new AllowAll()]).Configure(o);
        var filter = o.Filters.Request.CallToolFilters[0];

        var next = CallHandler(new CallToolResult
        {
            Content = [new TextContentBlock { Text = "ok" }]
        });
        var result = await filter(next)(CreateContext(new CallToolRequestParams { Name = "x" }), CancellationToken.None);

        await Assert.That(result.IsError).IsNotEqualTo(true);
    }

    [Test]
    public async Task CallToolFilter_NoName_PassesThrough()
    {
        var o = new McpServerOptions();
        Create([new DenyAll()]).Configure(o);
        var filter = o.Filters.Request.CallToolFilters[0];

        var next = CallHandler(new CallToolResult());
        var result = await filter(next)(CreateContext(new CallToolRequestParams { Name = null! }), CancellationToken.None);

        await Assert.That(result.IsError).IsNotEqualTo(true);
    }

    [Test]
    public async Task ListToolsFilter_MultipleStrategies_AndSemantics()
    {
        var o = new McpServerOptions();
        Create([new AllowOnly("a", "b"), new AllowOnly("b", "c")]).Configure(o);
        var filter = o.Filters.Request.ListToolsFilters[0];

        var next = ToolsHandler(new ListToolsResult
        {
            Tools = [new Tool { Name = "a" }, new Tool { Name = "b" }, new Tool { Name = "c" }]
        });
        var result = await filter(next)(CreateContext(new ListToolsRequestParams()), CancellationToken.None);

        await Assert.That(result.Tools).Count().IsEqualTo(1);
        await Assert.That(result.Tools![0].Name).IsEqualTo("b");
    }

    private static McpRequestHandler<ListToolsRequestParams, ListToolsResult> ToolsHandler(ListToolsResult r)
        => (_, _) => ValueTask.FromResult(r);

    private static McpRequestHandler<CallToolRequestParams, CallToolResult> CallHandler(CallToolResult r)
        => (_, _) => ValueTask.FromResult(r);

    private sealed class AllowAll : McpPrimitiveFilteringStrategy
    {
        protected override IEnumerable<string> FilterTools(IEnumerable<string> n) => n;
    }

    private sealed class DenyAll : McpPrimitiveFilteringStrategy
    {
        protected override IEnumerable<string> FilterTools(IEnumerable<string> n) => [];
    }

    private sealed class AllowOnly(params string[] allowed) : McpPrimitiveFilteringStrategy
    {
        protected override IEnumerable<string> FilterTools(IEnumerable<string> n) => n.Where(allowed.Contains);
    }
}
