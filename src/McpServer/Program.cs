using McpServer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvFile();

builder.AddLogging();

builder.Services
  .AddOAuth(builder.Configuration)
  .ConfigureRateLimiter(builder.Configuration)
  .AddMcp(builder.Configuration);

var app = builder.Build();

app
  .UseLogging()
  .UseOAuth()
  .UseMcp()
  .UseMaps();
await app.RunAsync();