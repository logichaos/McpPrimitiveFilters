using AuthenticatedHttpMcpServer.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

GlobalConfigurations.ApiSettings = builder.Configuration.GetRequiredSection("ApiSettings").Get<SettingsModel>()!;

builder.AddLoggingServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMcp();
builder.Services.AddRateLimitServices();
builder.Services.AddHealthChecks();

builder.Services.AddAuthServices(builder.Environment);

WebApplication app = builder.Build();

app.UseAuthorization();
app.UseRateLimiter();

app.MapMcp("/mcp")
  .RequireAuthorization()
  .RequireRateLimiting(Constants.RateLimit.Policies.Fixed);

app.MapHealthChecks("/health")
  .RequireAuthorization(Constants.Auth.Policies.MrAwesome)
  .RequireRateLimiting(Constants.RateLimit.Policies.Fixed);

app.Run();