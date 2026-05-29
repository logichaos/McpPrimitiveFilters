## 1. Implement Utility Tools

- [x] 1.1 Add `GetTimestamp` method — returns `DateTime.UtcNow.ToString("O")` with `[McpServerTool]` and `[Description]` attributes
- [x] 1.2 Add `Echo` method — accepts `string message` parameter, returns it unchanged with `[McpServerTool]` and `[Description]` attributes

## 2. Implement Admin Tools

- [x] 2.1 Define `DemoUser` record type with `Username` and `Role` string properties for `ListUsers` return data
- [x] 2.2 Define `ServerStats` record type with `Uptime` (string) and `RequestCount` (int) properties for `GetServerStats` return data
- [x] 2.3 Add `ListUsers` method — returns a hardcoded list of 3+ `DemoUser` objects with `[McpServerTool]` and `[Description]` attributes
- [x] 2.4 Add `GetServerStats` method — returns a `ServerStats` object with simulated uptime and request count, with `[McpServerTool]` and `[Description]` attributes

## 3. Unit Tests

- [x] 3.1 Add unit tests for `GetTimestamp` — verify returns valid ISO 8601 string parseable by `DateTime.Parse`
- [x] 3.2 Add unit tests for `Echo` — verify it echoes back the input message (including empty string case)
- [x] 3.3 Add unit tests for `ListUsers` — verify it returns 3+ users, each with non-empty `Username` and `Role`
- [x] 3.4 Add unit tests for `GetServerStats` — verify it returns `Uptime` (non-empty string) and `RequestCount` (non-negative int)

## 4. Integration Tests

- [x] 4.1 Add integration test: MCP client discovers all new tools (GetTimestamp, Echo, ListUsers, GetServerStats) via tool list
- [x] 4.2 Add integration test: invoke `GetTimestamp` via MCP client and verify result is a valid ISO 8601 timestamp
- [x] 4.3 Add integration test: invoke `Echo` via MCP client and verify correct echo response
- [x] 4.4 Add integration test: invoke `ListUsers` via MCP client and verify returned user list structure
- [x] 4.5 Add integration test: invoke `GetServerStats` via MCP client and verify returned stats structure

## 5. Validation

- [x] 5.1 Build the project (`dotnet build`) and verify no compilation errors
- [x] 5.2 Run all tests (`dotnet test`) to confirm all pass and no regressions
