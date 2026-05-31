using McpServer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddLogging();

builder.Services
  .AddOAuth(builder.Configuration)
  .ConfigureRateLimiter(builder.Configuration)
  .AddMcp(builder.Configuration)
  .AddToolFiltering();

var app = builder.Build();

app
  .UseLogging()
  .UseOAuth()
  .UseMcp()
  .UseMaps();
await app.RunAsync();