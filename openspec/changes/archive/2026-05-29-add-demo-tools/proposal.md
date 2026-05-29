## Why

The MCP server currently exposes only a single demo tool (`GetRandomNumber`), which is insufficient for testing and demonstrating the full capabilities of the authenticated HTTP MCP server. Adding more tools—including admin-oriented ones—validates tool authorization, OAuth scoping, and provides richer demo content.

## What Changes

- Add 4 new MCP server tools to the existing `RandomNumberTools` class:
  - **GetTimestamp** — returns the current UTC timestamp as ISO 8601
  - **Echo** — echoes back the input text the client sends
  - **ListUsers** — returns a simulated list of users (admin-sounding tool)
  - **GetServerStats** — returns simulated server statistics like uptime and request count (admin-sounding tool)

## Capabilities

### New Capabilities
- `demo-utility-tools`: Two general-purpose demo tools (GetTimestamp, Echo) for testing basic MCP interactions
- `demo-admin-tools`: Two admin-sounding demo tools (ListUsers, GetServerStats) for testing authorization and scoping scenarios

### Modified Capabilities
<!-- None — no existing specs to modify -->

## Impact

- Affected code: `src/McpServer/Tools/RandomNumberTools.cs`
- No new dependencies — uses only .NET BCL (no NuGet packages needed)
- No breaking changes — existing `GetRandomNumber` tool is preserved
- Tests: new integration tests may be added to verify tool discovery and invocation
