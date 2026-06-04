using McpServer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddLogging();

builder.Services
  .AddErrorHandling()
  .AddOAuth(builder.Configuration)
  .ConfigureRateLimiter(builder.Configuration)
  .AddMcp(builder.Configuration)
  .AddToolFiltering()
  .AddResourceFiltering();

var app = builder.Build();

app
  .UseErrorHandling()
  .UseLogging()
  .UseOAuth()
  .UseMcp()
  .UseMaps();
await app.RunAsync();