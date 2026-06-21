using McpServer.Infrastructure.OAuth;

namespace McpServer.Infrastructure;

internal sealed class EmbeddedOAuthServerHostedService : IHostedService
{
  private readonly OAuthOptions _oauthOptions;
  private readonly IConfiguration _configuration;
  private readonly ILogger<EmbeddedOAuthServerHostedService> _logger;
  private WebApplication? _app;

  public EmbeddedOAuthServerHostedService(
      OAuthOptions oauthOptions,
      IConfiguration configuration,
      ILogger<EmbeddedOAuthServerHostedService> logger)
  {
    _oauthOptions = oauthOptions;
    _configuration = configuration;
    _logger = logger;
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    if (_oauthOptions.EmbeddedOAuthServer is not { Enabled: true })
      return;

    _logger.LogInformation("Starting embedded OAuth server");

    _app = ModelContextProtocol.TestOAuthServer.Program.BuildApp(configuration: _configuration);
    await _app.StartAsync(cancellationToken);
  }

  public async Task StopAsync(CancellationToken cancellationToken)
  {
    if (_app is not null)
    {
      await _app.StopAsync(cancellationToken);
      await _app.DisposeAsync();
    }
  }
}
