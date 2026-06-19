using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace McpServer.Integration.Tests.Infrastructure.KestrelInMemory;

public abstract class KestrelInMemoryTest : IDisposable
{
  public KestrelInMemoryTest()
  {
    Builder = WebApplication.CreateEmptyBuilder(new());
    Builder.Services.AddSingleton<IConnectionListenerFactory>(KestrelInMemoryTransport);
    Builder.WebHost.UseKestrelCore();
    Builder.Services.AddRoutingCore();
    Builder.Services.AddLogging();
    Builder.Logging.SetMinimumLevel(LogLevel.Debug);

    SocketsHttpHandler.ConnectCallback = (context, token) =>
    {
      var connection = KestrelInMemoryTransport.CreateConnection(context.DnsEndPoint);
      return new(connection.ClientStream);
    };

    HttpClient = new HttpClient(SocketsHttpHandler);
    ConfigureHttpClient(HttpClient);
  }

  public WebApplicationBuilder Builder { get; }

  public HttpClient HttpClient { get; set; }

  public SocketsHttpHandler SocketsHttpHandler { get; } = new();

  public KestrelInMemoryTransport KestrelInMemoryTransport { get; } = new();

  protected static void ConfigureHttpClient(HttpClient httpClient)
  {
    httpClient.BaseAddress = new Uri("http://localhost:5000/");
    httpClient.Timeout = TestConstants.HttpClientTimeout;
  }

  public void Dispose()
  {
    HttpClient.Dispose();
  }
}