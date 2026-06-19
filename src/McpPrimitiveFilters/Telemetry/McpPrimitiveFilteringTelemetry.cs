using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace McpPrimitiveFilters.Telemetry;

internal static class McpPrimitiveFilteringTelemetry
{
  public const string SourceName = "McpPrimitiveFilters";
  public const string SourceVersion = "0.1.0";

  public static readonly ActivitySource ActivitySource = new(SourceName, SourceVersion);
  public static readonly Meter Meter = new(SourceName, SourceVersion);

  public const string TagPrimitiveType = "mcp.primitive.type";
  public const string TagPrimitiveName = "mcp.primitive.name";
  public const string TagOperation = "mcp.filter.operation";
  public const string TagAllowedCount = "mcp.filter.allowed";
  public const string TagDeniedCount = "mcp.filter.denied";

  public static readonly Counter<long> FilterCalls = Meter.CreateCounter<long>(
      "mcp.filter.calls",
      description: "Number of filter operations executed");

  public static readonly Counter<long> FilterDenials = Meter.CreateCounter<long>(
      "mcp.filter.denials",
      description: "Number of primitives denied by filters");

  public static readonly Histogram<double> FilterDuration = Meter.CreateHistogram<double>(
      "mcp.filter.duration",
      unit: "ms",
      description: "Filter operation duration in milliseconds");
}