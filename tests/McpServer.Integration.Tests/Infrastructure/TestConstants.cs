namespace McpServer.Integration.Tests.Infrastructure;

public static class TestConstants
{
  public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

  public static readonly TimeSpan HttpClientTimeout = TimeSpan.FromSeconds(60);
  public static readonly TimeSpan HttpClientPollingTimeout = TimeSpan.FromSeconds(2);
}