using McpPrimitiveFilters.Logging;

using Microsoft.Extensions.Options;

namespace McpPrimitiveFilters.Strategies;

public sealed class AppSettingsPrimitiveFilteringStrategy : McpPrimitiveFilteringStrategy
{
  private readonly IOptions<McpFilteringOptions> _options;
  private readonly ILogger<AppSettingsPrimitiveFilteringStrategy> _logger;

  public AppSettingsPrimitiveFilteringStrategy(IOptions<McpFilteringOptions> options, ILogger<AppSettingsPrimitiveFilteringStrategy> logger)
      => (_options, _logger) = (options, logger);

  protected override IEnumerable<string> FilterTools(IEnumerable<string> names)
      => ApplyAllowlist(_options.Value.Allowed.Tools, McpPrimitiveType.Tool, names);

  protected override IEnumerable<string> FilterResources(IEnumerable<string> names)
      => ApplyAllowlist(_options.Value.Allowed.Resources, McpPrimitiveType.Resource, names);

  protected override IEnumerable<string> FilterPrompts(IEnumerable<string> names)
      => ApplyAllowlist(_options.Value.Allowed.Prompts, McpPrimitiveType.Prompt, names);

  private IEnumerable<string> ApplyAllowlist(string[] allowed, McpPrimitiveType type, IEnumerable<string> names)
  {
    var namesList = names.ToList();

    McpFilteringLogMessages.AppSettingsIncoming(_logger, type, namesList.Count, string.Join(", ", namesList));
    McpFilteringLogMessages.AppSettingsAllowedConfig(_logger, type, allowed.Length, string.Join(", ", allowed));

    if (allowed.Length == 0)
      return namesList;

    var allowedSet = new HashSet<string>(allowed, StringComparer.OrdinalIgnoreCase);
    foreach (var allowedPrimitive in allowedSet)
      McpFilteringLogMessages.Allowed(_logger, type, "appSettings", allowedPrimitive);
    return namesList.Where(allowedSet.Contains);
  }
}