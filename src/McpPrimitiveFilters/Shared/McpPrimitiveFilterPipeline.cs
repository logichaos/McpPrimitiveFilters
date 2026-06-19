using System.Diagnostics;
using McpPrimitiveFilters.Logging;
using McpPrimitiveFilters.Telemetry;

namespace McpPrimitiveFilters;

internal static class McpPrimitiveFilterPipeline
{
    public const string OpList = "list";
    public const string OpCall = "call";
    public const string OpRead = "read";
    public const string OpGet  = "get";

    public static List<T> Apply<T>(
        McpPrimitiveType type,
        string operation,
        IList<T> items,
        Func<T, string> getName,
        McpPrimitiveFilteringStrategy[] strategies,
        ILogger logger)
    {
        using var activity = McpPrimitiveFilteringTelemetry.ActivitySource.StartActivity(
            $"filter {type.ToString().ToLowerInvariant()}s {operation}");
        activity?.SetTag(McpPrimitiveFilteringTelemetry.TagPrimitiveType, type.ToString());
        activity?.SetTag(McpPrimitiveFilteringTelemetry.TagOperation, operation);

        var sw = Stopwatch.StartNew();

        if (strategies.Length == 0)
        {
            sw.Stop();
            RecordMetrics(type, operation, 0, items.Count, sw.Elapsed.TotalMilliseconds);
            activity?.SetTag(McpPrimitiveFilteringTelemetry.TagAllowedCount, items.Count);
            activity?.SetTag(McpPrimitiveFilteringTelemetry.TagDeniedCount, 0);
            return [.. items];
        }

        var names = items.Select(getName).ToList();
        foreach (var s in strategies)
        {
            names = [.. s.FilterPrimitives(type, names)];
            if (names.Count == 0) break;
        }

        var allowedSet = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        var result = items
            .Where(item => allowedSet.Contains(getName(item)))
            .ToList();

        sw.Stop();

        var denied = items.Count - result.Count;
        RecordMetrics(type, operation, denied, result.Count, sw.Elapsed.TotalMilliseconds);

        activity?.SetTag(McpPrimitiveFilteringTelemetry.TagAllowedCount, result.Count);
        activity?.SetTag(McpPrimitiveFilteringTelemetry.TagDeniedCount, denied);

        if (denied > 0)
            McpFilteringLogMessages.Result(logger, type, result.Count, denied);

        return result;
    }

    public static bool Allows(
        string name,
        McpPrimitiveType type,
        string operation,
        McpPrimitiveFilteringStrategy[] strategies,
        ILogger logger)
    {
        using var activity = McpPrimitiveFilteringTelemetry.ActivitySource.StartActivity(
            $"check {type.ToString().ToLowerInvariant()} {operation}");
        activity?.SetTag(McpPrimitiveFilteringTelemetry.TagPrimitiveType, type.ToString());
        activity?.SetTag(McpPrimitiveFilteringTelemetry.TagPrimitiveName, name);
        activity?.SetTag(McpPrimitiveFilteringTelemetry.TagOperation, operation);

        var sw = Stopwatch.StartNew();

        if (strategies.Length == 0)
        {
            sw.Stop();
            RecordMetrics(type, operation, 0, 1, sw.Elapsed.TotalMilliseconds);
            return true;
        }

        var names = new[] { name }.AsEnumerable();
        foreach (var s in strategies)
            names = s.FilterPrimitives(type, names);

        var result = names.Any();

        sw.Stop();

        var denied = result ? 0 : 1;
        RecordMetrics(type, operation, denied, result ? 1 : 0, sw.Elapsed.TotalMilliseconds);

        if (!result)
            McpFilteringLogMessages.CallDenied(logger, type, null, name);

        return result;
    }

    private static void RecordMetrics(McpPrimitiveType type, string operation,
        int denied, int allowed, double durationMs)
    {
        var tags = new TagList
        {
            { McpPrimitiveFilteringTelemetry.TagPrimitiveType, type.ToString() },
            { McpPrimitiveFilteringTelemetry.TagOperation, operation }
        };

        McpPrimitiveFilteringTelemetry.FilterCalls.Add(1, tags);
        McpPrimitiveFilteringTelemetry.FilterDuration.Record(durationMs, tags);
        if (denied > 0)
            McpPrimitiveFilteringTelemetry.FilterDenials.Add(denied, tags);
    }
}
