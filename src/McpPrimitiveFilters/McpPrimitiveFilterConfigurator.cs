using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Server;

namespace McpPrimitiveFilters;

internal abstract class McpPrimitiveFilterConfigurator : IConfigureOptions<McpServerOptions>
{
  private readonly bool _enabled;

  protected readonly McpPrimitiveFilteringStrategy[] Strategies;
  protected readonly McpPrimitiveFiltersOptions Options;
  protected readonly ILogger Logger;

  protected McpPrimitiveFilterConfigurator(
      IEnumerable<McpPrimitiveFilteringStrategy> strategies,
      IOptions<McpPrimitiveFiltersOptions> options,
      ILoggerFactory loggerFactory,
      bool enabled,
      string loggerCategory)
  {
    Strategies = [.. strategies];
    Options = options.Value;
    Logger = loggerFactory.CreateLogger($"{nameof(McpPrimitiveFilters)}.{loggerCategory}");
    _enabled = enabled;
  }

  public void Configure(McpServerOptions o)
  {
    if (!_enabled) return;
    RegisterFilters(o);
  }

  protected abstract void RegisterFilters(McpServerOptions options);

  protected List<T> FilterByName<T>(
      McpPrimitiveType type,
      string operation,
      IList<T> items,
      Func<T, string> getName)
      => McpPrimitiveFilterPipeline.Apply(type, operation, items, getName, Strategies, Logger);

  protected bool Allows(
      string name,
      McpPrimitiveType type,
      string operation)
      => McpPrimitiveFilterPipeline.Allows(name, type, operation, Strategies, Logger);
}