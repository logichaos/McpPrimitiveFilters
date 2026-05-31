## Context

The MCP server project currently has a single `RandomNumberTools` class at `src/McpServer/Tools/RandomNumberTools.cs` containing one tool (`GetRandomNumber`). Tools are declared as methods annotated with `[McpServerTool]` and parameter descriptions via `[Description]`. The project uses the `ModelContextProtocol.Server` NuGet package. No authorization scoping is configured on tools today — all tools are available to any authenticated client.

## Goals / Non-Goals

**Goals:**
- Add 4 new MCP tools within the same `RandomNumberTools` class
- 2 general-purpose utility tools that return simple computed/echoed values
- 2 admin-sounding tools that return simulated data (mock users, mock stats)
- Keep implementation simple — all tools return hardcoded/simulated data, no real backend

**Non-Goals:**
- No real user management or server monitoring backend
- No OAuth scope enforcement for admin tools in this change (only the tool definitions)
- No renaming or restructuring the existing `RandomNumberTools` class
- No new NuGet dependencies

## Decisions

1. **All tools in `RandomNumberTools.cs`**
   - *Why*: The class is a demo/tools class by nature. Adding tools here keeps the change minimal and co-locates demo tools.
   - *Alternative*: A new file or class was considered but rejected — overkill for 4 simple methods.

2. **Admin tools use simulated static data**
   - *Why*: These are demo tools for testing, not production features. Hardcoded lists and counters are intentional.
   - *Alternative*: Connecting to real services would add dependencies and complexity beyond the change's scope.

3. **Return types: primitive/record types only**
   - *Why*: MCP serializes results as JSON. Using `string`, `int`, and simple records ensures clean serialization without extra setup.
   - *Alternative*: Complex DTOs or `JsonElement` would add unnecessary ceremony.

4. **No authorization attributes on admin tools yet**
   - *Why*: Scoping/authorization is a separate concern. This change only defines the tools; a future change could add `[Authorize]` or similar attributes.
   - *Alternative*: Adding auth now would couple this change to an auth design that isn't yet defined.

## Risks / Trade-offs

- **[Risk] Admin tools return fake data**: Clients might mistake `ListUsers` or `GetServerStats` for real data sources.
  → **Mitigation**: Tool descriptions clearly indicate "demo" or "simulated" nature.
- **[Risk] Class name `RandomNumberTools` is misleading with 5 tools**: The name becomes less descriptive.
  → **Mitigation**: Acceptable for now; a future cleanup could rename to `DemoTools` if desired.
