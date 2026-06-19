using McpServer.Infrastructure;

using Microsoft.Extensions.Logging;

using ModelContextProtocol.Protocol;

namespace McpServer.Unit.Tests;

public class DynamicLogLevelServiceTests
{
  [Test]
  public async Task DefaultMinLevel_IsInformation()
  {
    var service = new DynamicLogLevelService();

    await Assert.That(service.MinLevel).IsEqualTo(LogLevel.Information);
  }

  [Test]
  public async Task MinLevel_CanBeChanged()
  {
    var service = new DynamicLogLevelService();

    service.MinLevel = LogLevel.Error;

    await Assert.That(service.MinLevel).IsEqualTo(LogLevel.Error);
  }

  [Test]
  public async Task MapMCPLevelToNetLevel_Debug_MapsToDebug()
  {
    var result = DynamicLogLevelService.MapMCPLevelToNetLevel(LoggingLevel.Debug);

    await Assert.That(result).IsEqualTo(LogLevel.Debug);
  }

  [Test]
  public async Task MapMCPLevelToNetLevel_Info_MapsToInformation()
  {
    var result = DynamicLogLevelService.MapMCPLevelToNetLevel(LoggingLevel.Info);

    await Assert.That(result).IsEqualTo(LogLevel.Information);
  }

  [Test]
  public async Task MapMCPLevelToNetLevel_Notice_MapsToInformation()
  {
    var result = DynamicLogLevelService.MapMCPLevelToNetLevel(LoggingLevel.Notice);

    await Assert.That(result).IsEqualTo(LogLevel.Information);
  }

  [Test]
  public async Task MapMCPLevelToNetLevel_Warning_MapsToWarning()
  {
    var result = DynamicLogLevelService.MapMCPLevelToNetLevel(LoggingLevel.Warning);

    await Assert.That(result).IsEqualTo(LogLevel.Warning);
  }

  [Test]
  public async Task MapMCPLevelToNetLevel_Error_MapsToError()
  {
    var result = DynamicLogLevelService.MapMCPLevelToNetLevel(LoggingLevel.Error);

    await Assert.That(result).IsEqualTo(LogLevel.Error);
  }

  [Test]
  public async Task MapMCPLevelToNetLevel_Critical_MapsToCritical()
  {
    var result = DynamicLogLevelService.MapMCPLevelToNetLevel(LoggingLevel.Critical);

    await Assert.That(result).IsEqualTo(LogLevel.Critical);
  }

  [Test]
  public async Task MapMCPLevelToNetLevel_Alert_MapsToCritical()
  {
    var result = DynamicLogLevelService.MapMCPLevelToNetLevel(LoggingLevel.Alert);

    await Assert.That(result).IsEqualTo(LogLevel.Critical);
  }

  [Test]
  public async Task MapMCPLevelToNetLevel_Emergency_MapsToCritical()
  {
    var result = DynamicLogLevelService.MapMCPLevelToNetLevel(LoggingLevel.Emergency);

    await Assert.That(result).IsEqualTo(LogLevel.Critical);
  }
}