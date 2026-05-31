## Context

The MCP server currently exposes all registered tools to every authenticated client. The `ListTools` request returns the complete set with no per-client filtering. The project already has OAuth/JWT authentication via `AddOAuth` with multiple provider types, and tools are registered via `WithTools<T>()` on the `McpServerBuilder`. The MCP C# SDK v1.3.0 provides `AddListToolsFilter` via `WithRequestFilters(...)`, which allows intercepting `ListTools` results and modifying the returned tool collection before it reaches the client.

## Goals / Non-Goals

**Goals:**
- Provide a pluggable `IToolFilteringStrategy` interface that accepts `HttpContext` and the current tool list, and returns a filtered subset
- Implement two built-in strategies:
  - **OAuth Claims Strategy**: includes only tools where the authenticated user has a JWT claim `mcp.tool.<tool_name>` with value `"true"`
  - **AppSettings Strategy**: includes only tools listed in `Mcp:AllowedTools` configuration (a JSON string array)
- Apply all registered strategies in sequence with AND semantics (a tool must pass every strategy to be visible)
- Allow third parties to register custom strategies by adding `IToolFilteringStrategy` implementations to DI
- Follow the existing `AddXxx`/`UseXxx` convention used by `AddOAuth`/`UseOAuth`

**Non-Goals:**
- No per-tool authorization attributes (that is already covered by the SDK's `AddAuthorizationFilters` + `[Authorize]`)
- No dynamic registration/unregistration of tools at runtime
- No filtering of prompts or resources — tools only
- No per-parameter or per-result filtering — the filtering decision is made at the tool level only

## Decisions

1. **Use `AddListToolsFilter` and `AddCallToolFilter` as the integration hooks**
   - *Why*: The MCP SDK's `WithRequestFilters` provides both `AddListToolsFilter` (for visibility) and `AddCallToolFilter` (for enforcement). Both run after ASP.NET Core authentication/authorization middleware, so `HttpContext.User` claims are available. Using both ensures that filtering is not just cosmetic — a tool hidden from `ListTools` is also blocked at invocation time.
   - *Alternative considered*: The SDK's `AddAuthorizationFilters` + `[Authorize]` attributes provide per-tool authorization, but that requires static attributes and doesn't integrate with the pluggable strategy model. The filter approach unifies visibility and enforcement under the same strategy pipeline.

2. **Strategy interface: `IToolFilteringStrategy` with `HttpContext` parameter**
   - *Why*: Passing `HttpContext` gives strategies full access to the authenticated user (claims, identity) and to `RequestServices` for resolving additional dependencies. This enables both claims-based and config-based strategies without coupling to specific DI patterns.
   - *Alternative considered*: Passing only `ClaimsPrincipal` would work for the OAuth strategy but would block config-based or custom strategies that need `IConfiguration` or other services.

3. **AND semantics: sequential filtering**
   - *Why*: Each strategy narrows the tool list further. If any strategy excludes a tool, it's excluded from the final result. This is the most predictable and secure composition model — adding more strategies can only restrict, never expand.
   - *Alternative considered*: OR semantics (union of all strategies' allowed tools) was rejected because it would mean one permissive strategy could override all restrictive ones, which is a security risk.

4. **Built-in strategies registered by default when tool filtering is enabled**
   - *Why*: Both OAuth claims and appsettings filtering are common needs. Registering them by default means operators only need to configure claims or settings to get filtering, with no code changes.
   - *Alternative considered*: Requiring explicit registration of each strategy in `Program.cs`. This would be more explicit but more verbose and error-prone. The current approach balances convenience with flexibility — users can still remove or replace strategies via DI decoration.

5. **OAuth Claims strategy is a no-op when user is not authenticated**
   - *Why*: When OAuth is not configured, there are no claims to check. Rather than requiring conditional strategy registration, the strategy simply returns all tools unchanged.
   - *Alternative considered*: Conditionally registering the strategy only when OAuth is active adds complexity in `Program.cs`. A no-op strategy is simpler.

6. **AppSettings strategy is a no-op when `Mcp:AllowedTools` is empty or missing**
   - *Why*: An empty or missing allowlist means "allow all". This is the safe default — adding the config section is opt-in.
   - *Alternative considered*: An empty allowlist meaning "allow none" would be a breaking change for anyone who deploys with default config.

7. **Call-level enforcement returns an error response, not an exception**
   - *Why*: When a `CallTool` request targets a tool blocked by the strategy pipeline, the server returns a `CallToolResult` with `IsError = true` and a descriptive message (e.g., "Tool 'X' is not authorized"). This follows MCP protocol conventions — errors are communicated in-band, not via HTTP 500. The client receives a structured error it can handle gracefully.
   - *Alternative considered*: Throwing an exception or short-circuiting the HTTP response would break the MCP JSON-RPC contract and potentially confuse compliant clients.

8. **Strategy resolution via DI (`IEnumerable<IToolFilteringStrategy>`)**
   - *Why*: The list tools filter resolves all registered `IToolFilteringStrategy` implementations from the request's service provider. This is the standard .NET pattern for plugin-style composition, and it mirrors how ASP.NET Core resolves multiple `IAuthenticationHandler` or `IExceptionFilter` instances.
   - *Alternative considered*: A static registry like `IOAuthSchemeConfigurator`'s dictionary. DI is cleaner because strategies may need constructor-injected services (e.g., `IConfiguration`).

## Risks / Trade-offs

- **[Risk] Performance impact of dual filtering**: Both `ListTools` and `CallTool` requests will resolve and execute the full strategy pipeline. For `CallTool`, a single tool name needs to be checked rather than filtering the entire list.
  → **Mitigation**: The strategies are singletons and their logic is O(1) per tool (string comparison / claim lookup). For `CallTool`, only one tool is checked. Even with many strategies, overhead is negligible for typical tool counts (<50). CallTool always checks all registered strategies — a tool must pass every strategy to be invoked.

- **[Risk] Performance impact of resolving strategies per request**: The filter resolves all `IToolFilteringStrategy` instances on every `ListTools` call.
  → **Mitigation**: Strategies are registered as singletons; the resolution itself is cheap. The filter logic is simple (string comparisons and claim lookups). For large tool lists (>100), consider caching in a future iteration.

- **[Risk] Claim naming conflicts**: The `mcp.tool.<name>` claim format is a convention. If another system uses similar claim names, tools could be unintentionally exposed or hidden.
  → **Mitigation**: Use the `mcp.tool.` prefix as a namespace. Document the naming convention clearly.

## Open Questions

<!-- None — all design decisions are resolved -->
