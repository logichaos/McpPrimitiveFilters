using AuthenticatedHttpMcpServer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

GlobalConfigurations.ApiSettings = builder.Configuration.GetRequiredSection("ApiSettings").Get<SettingsModel>()!;

builder.Services.AddMcp();

builder.Services.AddHttpContextAccessor();
builder.Services.AddRateLimitServices();
builder.Services.AddHealthChecks();
builder.AddLoggingServices();

builder.Services.AddAuthServices(builder.Environment);

var app = builder.Build();

app.UseAuthorization();
app.UseRateLimiter();

app.MapMcp("/mcp")
  .RequireAuthorization()
  .RequireRateLimiting(Constants.RateLimit.Policies.Fixed);

app.MapHealthChecks("/health")
  .RequireAuthorization(Constants.Auth.Policies.McpSubscription)
  .RequireRateLimiting(Constants.RateLimit.Policies.Fixed);

app.Run();