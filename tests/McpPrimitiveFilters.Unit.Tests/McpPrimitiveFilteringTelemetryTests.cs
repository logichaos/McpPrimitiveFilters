using System.Diagnostics;
using System.Diagnostics.Metrics;

using McpPrimitiveFilters;
using McpPrimitiveFilters.Telemetry;

using Microsoft.Extensions.Logging.Abstractions;

using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpPrimitiveFilters.Unit.Tests;

[NotInParallel]
public class McpPrimitiveFilteringTelemetryTests
{
  [Test]
  public async Task ActivitySource_HasCorrectName()
  {
    await Assert.That(McpPrimitiveFilteringTelemetry.ActivitySource.Name)
        .IsEqualTo("McpPrimitiveFilters");
    await Assert.That(McpPrimitiveFilteringTelemetry.ActivitySource.Version)
        .IsEqualTo("0.1.0");
  }

  [Test]
  public async Task Meter_HasCorrectName()
  {
    await Assert.That(McpPrimitiveFilteringTelemetry.Meter.Name)
        .IsEqualTo("McpPrimitiveFilters");
    await Assert.That(McpPrimitiveFilteringTelemetry.Meter.Version)
        .IsEqualTo("0.1.0");
  }

  [Test]
  public async Task Apply_CreatesActivity_WithTags()
  {
    string? operationName = null;
    var tags = new Dictionary<string, string?>();

    using var listener = new ActivityListener
    {
      ShouldListenTo = s => s.Name == McpPrimitiveFilteringTelemetry.SourceName,
      Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
      ActivityStopped = a =>
      {
        operationName = a.DisplayName;
        foreach (var t in a.Tags)
          tags[t.Key] = t.Value;
      }
    };
    ActivitySource.AddActivityListener(listener);

    var items = new List<Tool> { new() { Name = "a" }, new() { Name = "b" } };
    McpPrimitiveFilterPipeline.Apply(
        McpPrimitiveType.Tool, "list", items, t => t.Name,
        [], NullLogger.Instance);

    await Assert.That(operationName).IsNotNull();
    await Assert.That(tags).ContainsKey(McpPrimitiveFilteringTelemetry.TagPrimitiveType);
    await Assert.That(tags[McpPrimitiveFilteringTelemetry.TagPrimitiveType]).IsEqualTo("Tool");
    await Assert.That(tags).ContainsKey(McpPrimitiveFilteringTelemetry.TagOperation);
    await Assert.That(tags[McpPrimitiveFilteringTelemetry.TagOperation]).IsEqualTo("list");
  }

  [Test]
  public async Task Allows_CreatesActivity_WithTags()
  {
    string? operationName = null;
    var tags = new Dictionary<string, string?>();

    using var listener = new ActivityListener
    {
      ShouldListenTo = s => s.Name == McpPrimitiveFilteringTelemetry.SourceName,
      Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
      ActivityStopped = a =>
      {
        operationName = a.DisplayName;
        foreach (var t in a.Tags)
          tags[t.Key] = t.Value;
      }
    };
    ActivitySource.AddActivityListener(listener);

    McpPrimitiveFilterPipeline.Allows(
        "SecretTool", McpPrimitiveType.Tool, "call",
        [], NullLogger.Instance);

    await Assert.That(operationName).IsNotNull();
    await Assert.That(tags).ContainsKey(McpPrimitiveFilteringTelemetry.TagPrimitiveType);
    await Assert.That(tags[McpPrimitiveFilteringTelemetry.TagPrimitiveType]).IsEqualTo("Tool");
    await Assert.That(tags).ContainsKey(McpPrimitiveFilteringTelemetry.TagPrimitiveName);
    await Assert.That(tags[McpPrimitiveFilteringTelemetry.TagPrimitiveName]).IsEqualTo("SecretTool");
    await Assert.That(tags).ContainsKey(McpPrimitiveFilteringTelemetry.TagOperation);
    await Assert.That(tags[McpPrimitiveFilteringTelemetry.TagOperation]).IsEqualTo("call");
  }

  [Test]
  public async Task Apply_AllDenied_RecordsMetrics()
  {
    long calls = 0, denials = 0;
    double duration = 0;

    using var listener = new MeterListener();
    listener.InstrumentPublished = (instrument, l) =>
    {
      if (instrument.Meter.Name == McpPrimitiveFilteringTelemetry.Meter.Name)
        l.EnableMeasurementEvents(instrument);
    };
    listener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
    {
      if (instrument.Name == "mcp.filter.calls") calls += measurement;
      if (instrument.Name == "mcp.filter.denials") denials += measurement;
    });
    listener.SetMeasurementEventCallback<double>((instrument, measurement, _, _) =>
    {
      if (instrument.Name == "mcp.filter.duration") duration += measurement;
    });
    listener.Start();

    var strategy = new DenyAllToolsStrategy();
    var items = new List<Tool> { new() { Name = "a" }, new() { Name = "b" } };
    McpPrimitiveFilterPipeline.Apply(
        McpPrimitiveType.Tool, "list", items, t => t.Name,
        [strategy], NullLogger.Instance);

    await Assert.That(calls).IsEqualTo(1);
    await Assert.That(denials).IsEqualTo(2);
    await Assert.That(duration).IsGreaterThan(0);
  }

  [Test]
  public async Task Allows_Denied_RecordsMetrics()
  {
    long calls = 0, denials = 0;

    using var listener = new MeterListener();
    listener.InstrumentPublished = (instrument, l) =>
    {
      if (instrument.Meter.Name == McpPrimitiveFilteringTelemetry.Meter.Name)
        l.EnableMeasurementEvents(instrument);
    };
    listener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
    {
      if (instrument.Name == "mcp.filter.calls") calls += measurement;
      if (instrument.Name == "mcp.filter.denials") denials += measurement;
    });
    listener.Start();

    var strategy = new DenyAllToolsStrategy();
    McpPrimitiveFilterPipeline.Allows(
        "SecretTool", McpPrimitiveType.Tool, "call",
        [strategy], NullLogger.Instance);

    await Assert.That(calls).IsEqualTo(1);
    await Assert.That(denials).IsEqualTo(1);
  }

  [Test]
  public async Task Meter_HasExpectedInstruments()
  {
    using var listener = new MeterListener();
    var instrumentNames = new List<string>();

    listener.InstrumentPublished = (instrument, _) =>
    {
      if (instrument.Meter.Name == McpPrimitiveFilteringTelemetry.Meter.Name)
        instrumentNames.Add(instrument.Name);
    };
    listener.Start();

    await Assert.That(instrumentNames).Contains("mcp.filter.calls");
    await Assert.That(instrumentNames).Contains("mcp.filter.denials");
    await Assert.That(instrumentNames).Contains("mcp.filter.duration");
  }

  [Test]
  public async Task Apply_NoDenials_DoesNotIncrementDenialCounter()
  {
    long denials = 0;

    using var listener = new MeterListener();
    listener.InstrumentPublished = (instrument, l) =>
    {
      if (instrument.Meter.Name == McpPrimitiveFilteringTelemetry.Meter.Name)
        l.EnableMeasurementEvents(instrument);
    };
    listener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
    {
      if (instrument.Name == "mcp.filter.denials") denials += measurement;
    });
    listener.Start();

    var items = new List<Tool> { new() { Name = "a" } };
    McpPrimitiveFilterPipeline.Apply(
        McpPrimitiveType.Tool, "list", items, t => t.Name,
        [], NullLogger.Instance);

    await Assert.That(denials).IsEqualTo(0);
  }

  private sealed class DenyAllToolsStrategy : McpPrimitiveFilteringStrategy
  {
    protected override IEnumerable<string> FilterTools(IEnumerable<string> n) => [];
  }
}