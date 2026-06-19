# McpPrimitiveFilters

Pluggable filtering for MCP server **tools**, **resources**, and **prompts**. Attach one or more strategies to decide which primitives each authenticated (or anonymous) client can see and invoke.

Comes with two built-in strategies: **appsettings allowlists** (for local/dev control) and **OAuth scope claims** (for enterprise authorization). You can add your own by extending `McpPrimitiveFilteringStrategy`.

## How to use

### Add the package and register

```csharp
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddMcpPrimitiveFilters();
```

This registers `IHttpContextAccessor`, three per-primitive configurators, and both built-in strategies (AppSettings + OAuth).

### With options

```csharp
builder.Services.AddMcpPrimitiveFilters(options =>
{
    // Use only OAuth scopes — disable appsettings allowlist
    options.UseBuiltinAppSettingsFilteringStrategy = false;

    // Make prompts publicly visible (no filtering)
    options.FilterPrompts = false;
});
```

| Option | Default | Description |
|---|---|---|
| `UseBuiltinAppSettingsFilteringStrategy` | `true` | Enables the `AppSettingsPrimitiveFilteringStrategy` |
| `UseBuiltinOAuthClaimsFilteringStrategy` | `true` | Enables the `OAuthClaimsFilteringStrategy` |
| `FilterTools` | `true` | Whether to filter tool lists and tool calls |
| `FilterResources` | `true` | Whether to filter resource lists and reads |
| `FilterPrompts` | `true` | Whether to filter prompt lists and gets |

### AppSettings allowlist

Configure which primitives are exposed via `appsettings.json`:

```jsonc
{
  "McpFiltering": {
    "Allowed": {
      "tools": ["GetRandomNumber", "Echo"],
      "resources": ["Server Info"],
      "prompts": null          // null = allow all prompts
    }
  }
}
```

An empty array means *nothing is allowed*. Omitting the key or setting it to `null` means *everything is allowed*.

### OAuth scope claims

When using OAuth, the strategy maps scope claims to primitives:

| Scope claim | Effect |
|---|---|
| `mcp.tools.all` | Allows **all** tools |
| `mcp.resources.all` | Allows **all** resources |
| `mcp.prompts.all` | Allows **all** prompts |
| `mcp.tool.<name>` | Allows the named tool (e.g. `mcp.tool.GetRandomNumber`) |
| `mcp.resource.<name>` | Allows the named resource |
| `mcp.prompt.<name>` | Allows the named prompt |

If the client is **not authenticated**, the OAuth strategy passes through all names — it only filters when a principal is present.

### Complete OAuth example

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(/* your config */);
builder.Services.AddAuthorization();

builder.Services.AddMcpPrimitiveFilters(options =>
{
    options.UseBuiltinAppSettingsFilteringStrategy = false;
});

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapMcp();
app.Run();
```

A client presenting a token with `scope: mcp.tool.Echo mcp.tool.Status` will only see and be able to call `Echo` and `Status`. Any other tool call returns an error with the message `"Tool 'X' is not authorized."`.

## Writing a custom strategy

Extend `McpPrimitiveFilteringStrategy` and override the methods for the primitive types you want to filter. The base class defaults to passing everything through, so you only override what you need.

```csharp
public sealed class TimeBasedFilteringStrategy : McpPrimitiveFilteringStrategy
{
    protected override IEnumerable<string> FilterTools(IEnumerable<string> names)
    {
        if (DateTime.Now.Hour is >= 9 and < 17)
            return names;
        return names.Where(n => !n.StartsWith("Admin", StringComparison.OrdinalIgnoreCase));
    }
}
```

If your strategy needs request context, inject `IHttpContextAccessor`:

```csharp
public sealed class RoleBasedFilteringStrategy : McpPrimitiveFilteringStrategy
{
    private readonly IHttpContextAccessor _http;

    public RoleBasedFilteringStrategy(IHttpContextAccessor http) => _http = http;

    protected override IEnumerable<string> FilterTools(IEnumerable<string> names)
    {
        var user = _http.HttpContext?.User;
        if (user?.IsInRole("Admin") == true) return names;
        return names.Where(n => !n.StartsWith("Admin"));
    }
}
```

Register it alongside the built-in strategies:

```csharp
builder.Services.AddMcpPrimitiveFilters();
builder.Services.AddSingleton<McpPrimitiveFilteringStrategy, TimeBasedFilteringStrategy>();
```

All `McpPrimitiveFilteringStrategy` registrations run in registration order as a pipeline — each strategy receives the output of the previous one, narrowing the allowlist. If any strategy returns an empty list, downstream strategies are skipped. Built-in strategies are registered via `TryAddEnumerable`, so they coexist with your custom ones. Use the options to disable built-ins you don't need.

## Logging

All strategy decisions are logged via `ILogger` under the category `McpPrimitiveFilters`:

| Level | Scenario |
|---|---|
| `Debug` | Per-primitive allow/deny, unauthenticated passthrough, scope details |
| `Information` | Wildcard scope grants, final allow/deny counts |
| `Warning` | Call denied at invocation time |

## Telemetry

The library emits OpenTelemetry-compatible signals using `System.Diagnostics.ActivitySource` and `Meter`. No additional NuGet packages are required — any OpenTelemetry collector (OTLP exporter, Jaeger, Zipkin, etc.) that instruments `ActivitySource` will pick up these signals automatically.

### Source

| Source | Name | Version |
|---|---|---|
| `ActivitySource` | `McpPrimitiveFilters` | `0.1.0` |
| `Meter` | `McpPrimitiveFilters` | `0.1.0` |

### Traces

Every filter operation creates an `Activity` span. The operation name reflects the action:

- `filter tools list` — filtering the tool list
- `check tool call` — checking whether a tool call is allowed
- `filter resources list` — filtering resources or resource templates
- `check resource read` — checking whether a resource read is allowed
- `filter prompts list` — filtering the prompt list
- `check prompt get` — checking whether a prompt get is allowed

Each span carries the following tags:

| Tag | Description |
|---|---|
| `mcp.primitive.type` | `Tool`, `Resource`, or `Prompt` |
| `mcp.primitive.name` | The primitive name (call/read/get checks only) |
| `mcp.filter.operation` | `list`, `call`, `read`, or `get` |
| `mcp.filter.allowed` | Number of primitives allowed (list operations) |
| `mcp.filter.denied` | Number of primitives denied (list operations) |

### Metrics

| Instrument | Type | Name | Description |
|---|---|---|---|
| Counter | `long` | `mcp.filter.calls` | Total filter operations executed |
| Counter | `long` | `mcp.filter.denials` | Total primitives denied |
| Histogram | `double` | `mcp.filter.duration` | Filter operation duration (ms) |

All metrics include `mcp.primitive.type` and `mcp.filter.operation` tags.

## License

MPL-2.0 — see the [LICENSE](../../LICENSE) file at the repository root.

## About this project

This is a project for me to learn more about the MCP server SDK, as well as the capabilities of MCP in .NET. I try hard to make it production-ready, but it should be taken **AS IS** — if you find bugs or have ideas, please open an issue and I'll do my best to address them.

If you have feedback about the structure, the approach, or anything else, I'd love to hear it. This is my first public repo and I'm still learning.
