## 1. Core Infrastructure

- [x] 1.1 Create `src/McpServer/Infrastructure/ToolFiltering/` directory
- [x] 1.2 Create `IToolFilteringStrategy.cs` — public interface in `McpServer.Infrastructure.ToolFiltering` namespace with single method `FilterTools(HttpContext httpContext, IEnumerable<McpServerTool> tools)`

## 2. OAuth Claims Strategy

- [x] 2.1 Create `OAuthClaimsToolFilteringStrategy.cs` — implements `IToolFilteringStrategy`
- [x] 2.2 Implement `FilterTools`: check `HttpContext.User.Identity.IsAuthenticated`; if not authenticated, return all tools unchanged
- [x] 2.3 For authenticated users, iterate tools and include only those where `User.FindFirst($"mcp.tool.{tool.Name}")?.Value == "true"`

## 3. AppSettings Strategy

- [x] 3.1 Create `AppSettingsToolFilteringStrategy.cs` — implements `IToolFilteringStrategy`, takes `IConfiguration` via constructor injection
- [x] 3.2 Implement `FilterTools`: read `Mcp:AllowedTools` config section as `string[]`
- [x] 3.3 If the array is null or empty, return all tools unchanged (no-op)
- [x] 3.4 Otherwise, return only tools whose `.Name` appears in the allowlist

## 4. Pipeline Wiring

- [x] 4.1 Add `AddToolFiltering(this IServiceCollection services)` extension method in a new file `ApiBuilder.ToolFiltering.cs` (or in `ApiBuilder.Mcp.cs`)
- [x] 4.2 In `AddToolFiltering`, register both built-in strategies as `Singleton<IToolFilteringStrategy, ...>` 
- [x] 4.3 Add `WithRequestFilters` → `AddListToolsFilter` that resolves all `IToolFilteringStrategy` from `context.Services`, applies them sequentially (AND semantics) to `result.Tools`, and replaces `result.Tools` with the filtered list
- [x] 4.4 Add `WithRequestFilters` → `AddCallToolFilter` that resolves all strategies, evaluates the target tool name against all strategies, and returns a `CallToolResult` with `IsError = true` and a descriptive message if any strategy blocks the tool

## 5. App Registration

- [x] 5.1 Update `Program.cs` to call `builder.Services.AddToolFiltering()` after the existing `AddMcp(builder.Configuration)` call

## 6. Unit Tests

- [x] 6.1 Create `OAuthClaimsToolFilteringStrategyTests.cs` in the unit test project
- [x] 6.2 Test: authenticated user with matching claims — only tools with corresponding `mcp.tool.<name> = "true"` claims are returned
- [x] 6.3 Test: authenticated user with no tool claims — returns empty list
- [x] 6.4 Test: unauthenticated user — returns all tools unchanged (no-op)
- [x] 6.5 Test: claim value is `"false"` — tool is excluded
- [x] 6.6 Create `AppSettingsToolFilteringStrategyTests.cs` in the unit test project
- [x] 6.7 Test: `Mcp:AllowedTools` contains a subset — only matching tools returned
- [x] 6.8 Test: `Mcp:AllowedTools` is empty array — returns all tools unchanged
- [x] 6.9 Test: `Mcp:AllowedTools` key is missing — returns all tools unchanged
- [x] 6.10 Test: `Mcp:AllowedTools` references a non-existent tool — returns empty list

## 7. Integration Tests — Visibility (ListTools)

- [x] 7.1 Add integration test: with `Mcp:AllowedTools` configured to allow only `GetRandomNumber` and `Echo`, `ListTools` returns exactly those two tools
- [x] 7.2 Add integration test: with no filtering configured, `ListTools` returns all registered tools (regression test)

## 8. Integration Tests — Enforcement (CallTool)

- [x] 8.1 Add integration test: with `Mcp:AllowedTools` allowing only `GetRandomNumber`, invoking `GetRandomNumber` via `CallTool` succeeds
- [x] 8.2 Add integration test: with `Mcp:AllowedTools` allowing only `GetRandomNumber`, invoking `Echo` via `CallTool` returns an error response with `IsError = true`
- [ ] 8.3 Add integration test: with OAuth claims allowing only `Echo`, invoking `Echo` succeeds and invoking `GetRandomNumber` returns an error **(deferred: requires TestOAuthServer support for custom JWT claims)**

## 9. Validation

- [x] 9.1 Build the project (`dotnet build`) and verify no compilation errors
- [x] 9.2 Run all tests (`dotnet test`) to confirm all pass and no regressions
