using FakeItEasy;

using McpPrimitiveFilters;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpPrimitiveFilters.Unit.Tests;

public class PromptFilterConfiguratorTests
{
  private static PromptFilterConfigurator Create(
      IEnumerable<McpPrimitiveFilteringStrategy>? strategies = null,
      McpPrimitiveFiltersOptions? options = null)
  {
    return new PromptFilterConfigurator(
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
  public async Task Configure_FilterPromptsEnabled_AddsPromptFilters()
  {
    var o = new McpServerOptions();
    Create().Configure(o);

    await Assert.That(o.Filters.Request.ListPromptsFilters).Count().IsEqualTo(1);
    await Assert.That(o.Filters.Request.GetPromptFilters).Count().IsEqualTo(1);
    await Assert.That(o.Filters.Request.ListToolsFilters).Count().IsEqualTo(0);
  }

  [Test]
  public async Task Configure_FilterPromptsDisabled_AddsNoFilters()
  {
    var o = new McpServerOptions();
    Create(options: new McpPrimitiveFiltersOptions { FilterPrompts = false }).Configure(o);

    await Assert.That(o.Filters.Request.ListPromptsFilters).Count().IsEqualTo(0);
    await Assert.That(o.Filters.Request.GetPromptFilters).Count().IsEqualTo(0);
  }

  [Test]
  public async Task ListPromptsFilter_NoStrategies_ReturnsAll()
  {
    var o = new McpServerOptions();
    Create().Configure(o);
    var filter = o.Filters.Request.ListPromptsFilters[0];

    var next = PromptListHandler(new ListPromptsResult
    {
      Prompts = [new Prompt { Name = "a" }, new Prompt { Name = "b" }]
    });
    var result = await filter(next)(CreateContext(new ListPromptsRequestParams()), CancellationToken.None);

    await Assert.That(result.Prompts).Count().IsEqualTo(2);
  }

  [Test]
  public async Task ListPromptsFilter_WithBlockingStrategy_Filters()
  {
    var o = new McpServerOptions();
    Create([new AllowOnly("a")]).Configure(o);
    var filter = o.Filters.Request.ListPromptsFilters[0];

    var next = PromptListHandler(new ListPromptsResult
    {
      Prompts = [new Prompt { Name = "a" }, new Prompt { Name = "b" }]
    });
    var result = await filter(next)(CreateContext(new ListPromptsRequestParams()), CancellationToken.None);

    await Assert.That(result.Prompts).Count().IsEqualTo(1);
    await Assert.That(result.Prompts![0].Name).IsEqualTo("a");
  }

  [Test]
  public async Task GetPromptFilter_Denied_ReturnsError()
  {
    var o = new McpServerOptions();
    Create([new DenyAll()]).Configure(o);
    var filter = o.Filters.Request.GetPromptFilters[0];

    var next = PromptGetHandler(new GetPromptResult());
    var result = await filter(next)(CreateContext(new GetPromptRequestParams { Name = "x" }), CancellationToken.None);

    await Assert.That(result.Messages).Count().IsEqualTo(1);
    var text = ((TextContentBlock)result.Messages![0].Content).Text;
    await Assert.That(text).Contains("not authorized");
  }

  [Test]
  public async Task GetPromptFilter_Allowed_DelegatesToNext()
  {
    var o = new McpServerOptions();
    Create([new AllowAll()]).Configure(o);
    var filter = o.Filters.Request.GetPromptFilters[0];

    var next = PromptGetHandler(new GetPromptResult
    {
      Messages = [new PromptMessage { Role = Role.User, Content = new TextContentBlock { Text = "ok" } }]
    });
    var result = await filter(next)(CreateContext(new GetPromptRequestParams { Name = "x" }), CancellationToken.None);

    await Assert.That(result.Messages).Count().IsEqualTo(1);
  }

  private static McpRequestHandler<ListPromptsRequestParams, ListPromptsResult> PromptListHandler(ListPromptsResult r)
      => (_, _) => ValueTask.FromResult(r);

  private static McpRequestHandler<GetPromptRequestParams, GetPromptResult> PromptGetHandler(GetPromptResult r)
      => (_, _) => ValueTask.FromResult(r);

  private sealed class AllowAll : McpPrimitiveFilteringStrategy
  {
    protected override IEnumerable<string> FilterPrompts(IEnumerable<string> n) => n;
  }

  private sealed class DenyAll : McpPrimitiveFilteringStrategy
  {
    protected override IEnumerable<string> FilterPrompts(IEnumerable<string> n) => [];
  }

  private sealed class AllowOnly(params string[] allowed) : McpPrimitiveFilteringStrategy
  {
    protected override IEnumerable<string> FilterPrompts(IEnumerable<string> n) => n.Where(allowed.Contains);
  }
}