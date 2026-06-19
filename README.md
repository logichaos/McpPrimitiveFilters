# MCP Primitive Filters

## credits

- [erwinkramer](https://github.com/erwinkramer) is the author of the [bank-api](https://github.com/erwinkramer/bank-api) project. If you don't know the repo, have a look at it. It's a great place to learn many things about writing APIs in AspNetCore.
  - A while back I was in need of refreshing my knowledge about modern .net web APIs and landed on that repo. It has been a huge influence on how I write web apps and a strong guideline on how to structure my code.
  - When I contacted erwinkramer about wanting to publish my MCP project that was influenced by what I learned from his repo, he not only told me that it was ok, he even took time to review my repo and gave me a lot of pointers and feedback. He made me realize a lot of things that I hadn't thought of and led to the decision to rewrite the whole thing from scratch.
- [Mario Zechner](https://mariozechner.at) is the author of the [PI coding agent](https://pi.dev). I love PI because, to me, it emphasizes the importance of keeping the context concise.
  - In some of his blogs, Mario delves into MCP servers and how they clutter your context with information about all the tools that you don't need. (please check out his blog for more info, it was a really good read for me and I think it could be for many other people)
  - What he said resonated with my strong conviction, that controlling the context is something that many people should understand, in order to get better results from AI models: too much might lead to hallucinations, too little might give wrong results.
  - That is why I thought of the idea of having an MCP server, where you could just choose which tools are exposed, to keep your context concise.

## The library — `McpPrimitiveFilters`

Pluggable filtering for MCP server **tools**, **resources**, and **prompts**. Attach one or more strategies to decide which primitives each client can see and invoke.

Comes with two built-in strategies: **appsettings allowlists** (for local/dev control) and **OAuth scope claims** (for enterprise setups). You can add your own by extending `McpPrimitiveFilteringStrategy`.

### Install and register

```csharp
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddMcpPrimitiveFilters();
```

This registers `IHttpContextAccessor`, three per-primitive configurators (tools, resources, prompts), and both built-in strategies.

### Options

```csharp
builder.Services.AddMcpPrimitiveFilters(options =>
{
    options.UseBuiltinAppSettingsFilteringStrategy = false; // only OAuth
    options.UseBuiltinOAuthClaimsFilteringStrategy = false;  // only AppSettings
    options.FilterTools = true;
    options.FilterResources = true;
    options.FilterPrompts = false;                           // prompts are public
});
```

| Option | Default | Description |
|---|---|---|
| `UseBuiltinAppSettingsFilteringStrategy` | `true` | Enables the `AppSettingsPrimitiveFilteringStrategy` |
| `UseBuiltinOAuthClaimsFilteringStrategy` | `true` | Enables the `OAuthClaimsFilteringStrategy` |
| `FilterTools` | `true` | Filter tool lists and tool calls |
| `FilterResources` | `true` | Filter resource lists and reads |
| `FilterPrompts` | `true` | Filter prompt lists and gets |

### AppSettings allowlist

```jsonc
{
  "McpFiltering": {
    "Allowed": {
      "tools": ["GetRandomNumber", "Echo"],
      "resources": ["Server Info"],
      "prompts": null          // null = allow all
    }
  }
}
```

An empty array means *nothing is allowed*. `null` or missing key means *everything is allowed*.

### OAuth scope claims

When using OAuth, the strategy maps scope claims to primitives:

| Scope claim | Effect |
|---|---|
| `mcp.tools.all` | Allows **all** tools |
| `mcp.resources.all` | Allows **all** resources |
| `mcp.prompts.all` | Allows **all** prompts |
| `mcp.tool.<name>` | Allows the named tool |
| `mcp.resource.<name>` | Allows the named resource |
| `mcp.prompt.<name>` | Allows the named prompt |

If the client is **not authenticated**, the OAuth strategy passes through all names — it only filters when a principal is present.

A client presenting a token with `scope: mcp.tool.Echo mcp.tool.Status` will only see and be able to call `Echo` and `Status`. Any other tool call returns `"Tool 'X' is not authorized."`.

### Writing a custom strategy

Extend `McpPrimitiveFilteringStrategy` and override the methods you need. The base class defaults to passing everything through.

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

Register it alongside the built-ins:

```csharp
builder.Services.AddMcpPrimitiveFilters();
builder.Services.AddSingleton<McpPrimitiveFilteringStrategy, TimeBasedFilteringStrategy>();
```

All strategies run in registration order as a pipeline — each receives the output of the previous one, narrowing the allowlist. If any strategy returns an empty list, downstream strategies are skipped. Built-in strategies use `TryAddEnumerable`, so they coexist with yours. Use the options to disable built-ins you don't need.

### Logging

All strategy decisions are logged via `ILogger` under the category `McpPrimitiveFilters`:

| Level | Scenario |
|---|---|
| `Debug` | Per-primitive allow/deny, unauthenticated passthrough, scope details |
| `Information` | Wildcard scope grants, final allow/deny counts |
| `Warning` | Call denied at invocation time |

### Telemetry

The library emits OpenTelemetry-compatible signals using `System.Diagnostics.ActivitySource` and `Meter`. No additional NuGet packages required — any OTEL collector (OTLP exporter, Jaeger, Zipkin, etc.) picks them up automatically.

| Source | Name |
|---|---|
| `ActivitySource` | `McpPrimitiveFilters` |
| `Meter` | `McpPrimitiveFilters` |

**Traces** — every filter operation creates a span (`filter tools list`, `check tool call`, `filter resources list`, `check resource read`, `filter prompts list`, `check prompt get`) with tags for primitive type, name, and operation.

**Metrics** — `mcp.filter.calls` (counter), `mcp.filter.denials` (counter), `mcp.filter.duration` (histogram, ms), all tagged with primitive type and operation.

## The sample server

The repo includes a sample HTTP MCP server in `samples/McpServer/` that demonstrates the library in action with OAuth, rate limiting, OpenTelemetry, and compliance redaction.

### Run it

```bash
dotnet run --project samples/McpServer
```

The server starts on the configured port and exposes:
- `POST /` — MCP protocol endpoint
- `GET /` — health check
- `GET /.well-known/oauth-authorization-server` — OAuth metadata (when OAuth is enabled)

### OAuth quick-start (development)

```bash
# Terminal 1 — start the test OAuth server
dotnet run --project tests/ModelContextProtocol.TestOAuthServer

# Terminal 2 — start the MCP server with InMemory auth
dotnet run --project samples/McpServer --environment Development
```

Three OAuth providers are built-in (enable one via `appsettings.json`):

| Provider | Best for | Config fields |
|---|---|---|
| `InMemory` | Local dev | `AuthorityUrl` |
| `EntraId` | Azure | `TenantId`, `ClientId` |
| `Auth0` | SaaS | `Domain`, `ClientId` |

### What's inside the sample

```
samples/
├── McpServer/                  # The server app
│   ├── Program.cs              # Startup — calls AddMcpPrimitiveFilters(), AddOAuth(), AddMcp()
│   ├── Tools/                  # Example tools (GetRandomNumber, Echo, GetTimestamp, etc.)
│   ├── Resources/              # Example resources + URI templates (weather://{city}, etc.)
│   └── Prompts/                # Example prompts
└── McpServer.Core/             # Cross-cutting infrastructure
    └── Infrastructure/         # OAuth configurators, rate limiting, logging, compliance
```

### Repo layout

```
├── src/McpPrimitiveFilters/    # The library
├── samples/                    # Sample server
├── tests/
│   ├── McpPrimitiveFilters.Unit.Tests/
│   ├── McpServer.Unit.Tests/
│   ├── McpServer.Integration.Tests/
│   └── ModelContextProtocol.TestOAuthServer/
```

## About the License

- Since it's my first time making a public repo, I am unsure what license to use. I wanted something that is open for people to use, but was thinking that it might be nice to have changes flow back into the repo, in order to help everyone.
- After a bit of research, it was suggested to me, that a MPL-2.0 would do exactly that, so I went with it.
- If you have any input on this, I would be very happy to hear it. I'm a total newbie here.

## Disclaimer

- This is a repo for me to learn more about the SDK, as well as the capabilities of MCP. I try hard to make it "production ready", so if you think something is missing, don't hesitate to create an issue.
- I can't promise to have time for all feedback, but, as long as I do, I will ;-)
- I used AI assisted code generation for the project. The way I use it is more of a "learner". If I get stuck on something, I brainstorm with the agent and I have a solution proposed/implemented, then I go through all generated code and try to understand how it works/propose changes, according to my engineering skills. Once I have a good grasp of what needs to be done, I scratch everything and start anew, with the learnings I have made from the previous run, along with unit and integration tests.
- I have created this code from scratch many times, and only when I reach a point that I am satisfied with, will I have a snapshot upon which I will do the whole rinse-and-repeat cycle all over again.
- This is my first public repo with code that I created. It's scary, but I would be very happy if anyone can use what I learned here to make their lives easier.
- Along with learning about MCP, I have decided to try and use [JJ vcs](https://www.jj-vcs.dev/latest/) for version control.
  - If you don't know anything about JJ, I encourage you to have a look, as it has proven to be really powerful and fun to use. This coming from someone who has been a hardcore git fan, using the awesome [LazyGit client](https://github.com/jesseduffield/lazygit). LazyGit is, for me, the best git client I have ever seen. It has upped my git game in so many ways and I'm forever grateful for Jesse for this amazing tool.
