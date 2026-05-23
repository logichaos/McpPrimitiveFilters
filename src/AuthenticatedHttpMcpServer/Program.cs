using AuthenticatedHttpMcpServer.Infrastructure;
using AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

GlobalConfigurations.ApiSettings = builder.Configuration.GetRequiredSection("ApiSettings").Get<SettingsModel>()!;
builder.Services.Configure<ToolsSelectionOptions>(builder.Configuration.GetSection(ToolsSelectionOptions.ToolsSelection));

builder.AddLoggingServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMcp();
builder.Services.AddRateLimitServices();
builder.Services.AddHealthChecks();

builder.Services.AddAuthServices(builder.Environment);

WebApplication app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapMcp("/mcp")
  .RequireAuthorization(Constants.Auth.Policies.Mcp)
  .RequireRateLimiting(Constants.RateLimit.Policies.Fixed);

app.MapHealthChecks("/health")
  .RequireAuthorization(Constants.Auth.Policies.MrAwesome)
  .RequireRateLimiting(Constants.RateLimit.Policies.Fixed);

app.Run();
