## Why

The MCP server currently exposes all registered tools to every authenticated client with no mechanism to restrict which tools a given client can access. This is a critical gap for multi-tenant or tiered-access scenarios where different OAuth clients or user roles should see different subsets of available tools. Providing a pluggable, extensible tool-filtering pipeline allows operators to control tool visibility via OAuth claims, configuration, or custom logic without modifying the core server.

## What Changes

- Introduce a `IToolFilteringStrategy` interface that accepts the current `HttpContext` and an `IEnumerable<McpServerTool>` (all registered tools), and returns the filtered subset
- Implement two built-in strategies:
  - **OAuth Claims Strategy**: Reads claims of the form `mcp.tool.<tool_name>` (with value `"true"`) from the authenticated user's JWT claims, and includes only tools whose corresponding claim exists and is truthy
  - **AppSettings Strategy**: Reads `Mcp:AllowedTools` from configuration (a JSON array of tool names), and includes only tools whose names appear in the list
- Wire strategies into the MCP pipeline so they are evaluated on every `ListTools` request (visibility filtering) and every `CallTool` request (enforcement), applying all registered strategies in sequence (AND semantics: a tool must pass all strategies to be included/invoked)
- Provide a `AddToolFiltering` / `UseToolFiltering` registration pattern at the app root, consistent with the existing `AddOAuth` / `UseOAuth` convention
- Allow third-party strategies to be registered via `RegisterToolFilteringStrategy()` on the service collection, mirroring the extensibility pattern of `IOAuthSchemeConfigurator`

## Capabilities

### New Capabilities
- `tool-filtering-strategies`: Core infrastructure for filtering MCP tools via a pluggable strategy pipeline—interface definition, registration, and the two built-in strategies (OAuth claims and appsettings allowlist)

### Modified Capabilities
<!-- None — no existing specs to modify -->

## Impact

- Affected code: `src/McpServer/Infrastructure/ApiBuilder.Mcp.cs` (strategy registration and pipeline), `src/McpServer/Program.cs` (registration call), new files under `src/McpServer/Infrastructure/ToolFiltering/`
- No new NuGet dependencies — uses existing `ModelContextProtocol.AspNetCore` and `Microsoft.AspNetCore.Authentication.JwtBearer`
- No breaking changes — existing tools and their registrations are unchanged; filtering only activates when explicitly configured
- Tests: new unit and integration tests for strategy evaluation, claim parsing, config parsing, and end-to-end tool listing with filtering
