# McpAuthorizationFiltering

Pluggable authorization filtering for MCP server tools, resources, and prompts.
One extension method call and you get OAuth scope-based filtering and
appsettings allowlists — for all three primitive types.

## Installation

```bash
dotnet add package McpAuthorizationFiltering
```

## Quick Start

```csharp
using Microsoft.Extensions.DependencyInjection;

services.AddMcpServer()
    .WithHttpTransport(opts => opts.Stateless = true)
    .WithTools<MyTools>()
    .WithResources<MyResources>()
    .AddMcpAuthorizationFiltering();
```

## Configuration

```jsonc
{
  "McpFiltering": {
    "Allowed": {
      "tools": ["get_random_number", "echo"],
      "resources": ["server://info"]
      // prompts omitted → all prompts visible
    }
  }
}
```

When a section is absent or empty, all primitives of that type are visible.

## OAuth Scope Claims

Claim patterns follow the convention:

| Primitive | Per-item scope            | Wildcard scope          |
|-----------|---------------------------|-------------------------|
| Tool      | `mcp.tool.{name}`         | `mcp.tools.all`         |
| Resource  | `mcp.resource.{name}`     | `mcp.resources.all`     |
| Prompt    | `mcp.prompt.{name}`       | `mcp.prompts.all`       |

Strategies apply with AND semantics — a primitive must pass all registered
strategies to be visible or invocable.

## Options

```csharp
.AddMcpAuthorizationFiltering(options =>
{
    options.AppSettingsEnabled = false;   // disable appsettings strategy
    options.OAuthClaimsEnabled = true;
    options.FilterPrompts = true;         // enable prompt filtering
})
```

| Option | Default | Description |
|--------|---------|-------------|
| `AppSettingsEnabled` | `true` | Register the AppSettings allowlist strategy |
| `OAuthClaimsEnabled` | `true` | Register the OAuth claims strategy |
| `FilterTools` | `true` | Wire tool request filters |
| `FilterResources` | `true` | Wire resource request filters |
| `FilterPrompts` | `false` | Wire prompt request filters |

## Custom Strategies

Implement `McpPrimitiveFilteringStrategy` and register it before calling
`AddMcpAuthorizationFiltering()`:

```csharp
public sealed class MyFilter : McpPrimitiveFilteringStrategy
{
    protected override IEnumerable<string> FilterTools(
        HttpContext ctx, IEnumerable<string> names)
        => names.Where(n => !n.StartsWith("admin_"));
}

// Register before the library call — AND-composed with built-in strategies
builder.Services.AddSingleton<McpPrimitiveFilteringStrategy, MyFilter>();
builder.AddMcpAuthorizationFiltering();
```

## How It Works

The library registers strategies in DI and wires MCP request filters
(`ListTools`, `CallTool`, `ListResources`, `ReadResource`, etc.) via
`WithRequestFilters`. Each strategy receives the current `HttpContext`
and a list of primitive names, returning the allowed subset. The
strategies are applied in registration order with AND semantics.

No reflection at request time. No custom attributes needed.
