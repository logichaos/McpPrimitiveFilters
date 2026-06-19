using McpPrimitiveFilters.Logging;

using Microsoft.Extensions.Configuration;

namespace McpPrimitiveFilters.Strategies;

public sealed class AppSettingsPrimitiveFilteringStrategy : McpPrimitiveFilteringStrategy
{
  private static readonly string[] Empty = [];

  private readonly IConfiguration _configuration;
  private readonly ILogger<AppSettingsPrimitiveFilteringStrategy> _logger;

  public AppSettingsPrimitiveFilteringStrategy(IConfiguration configuration, ILogger<AppSettingsPrimitiveFilteringStrategy> logger)
      => (_configuration, _logger) = (configuration, logger);

  protected override IEnumerable<string> FilterTools(IEnumerable<string> names)
      => ApplyAllowlist("McpFiltering:Allowed:tools", McpPrimitiveType.Tool, names);

  protected override IEnumerable<string> FilterResources(IEnumerable<string> names)
      => ApplyAllowlist("McpFiltering:Allowed:resources", McpPrimitiveType.Resource, names);

  protected override IEnumerable<string> FilterPrompts(IEnumerable<string> names)
      => ApplyAllowlist("McpFiltering:Allowed:prompts", McpPrimitiveType.Prompt, names);

  private IEnumerable<string> ApplyAllowlist(string configKey, McpPrimitiveType type, IEnumerable<string> names)
  {
    var allowedConfig = _configuration.GetSection(configKey).Get<string[]>() ?? Empty;

    if (allowedConfig.Length == 0)
      return names;

    var allowedSet = new HashSet<string>(allowedConfig, StringComparer.OrdinalIgnoreCase);
    foreach (var allowedPrimitive in allowedSet)
      McpFilteringLogMessages.Allowed(_logger, type, "appSettings", allowedPrimitive);
    return names.Where(allowedSet.Contains);
  }
}
