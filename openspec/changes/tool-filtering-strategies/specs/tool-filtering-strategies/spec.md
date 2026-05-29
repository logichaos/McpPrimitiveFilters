## ADDED Requirements

### Requirement: IToolFilteringStrategy interface
The system SHALL define a public interface `IToolFilteringStrategy` in the `McpServer.Infrastructure.ToolFiltering` namespace with a single method `FilterTools` that accepts an `HttpContext` and an `IEnumerable<McpServerTool>`, and returns an `IEnumerable<McpServerTool>` representing the filtered subset.

#### Scenario: Strategy receives full tool list
- **WHEN** a strategy's `FilterTools` method is invoked with an `HttpContext` and a list of 5 tools
- **THEN** the method receives all 5 tools as input

#### Scenario: Strategy returns filtered subset
- **WHEN** a strategy's `FilterTools` method returns only 2 tools from an input of 5
- **THEN** the returned enumerable contains exactly those 2 tools

### Requirement: OAuth Claims tool filtering strategy
The system SHALL provide a built-in `OAuthClaimsToolFilteringStrategy` that implements `IToolFilteringStrategy` and filters tools based on JWT claims of the form `mcp.tool.<tool_name>` present on the authenticated user. A tool is included if and only if the user has a claim with the exact key `mcp.tool.<tool_name>` and value `"true"` (case-sensitive for the value).

#### Scenario: Authenticated user has matching claims
- **WHEN** an authenticated user has claims `mcp.tool.GetRandomNumber = "true"` and `mcp.tool.Echo = "true"`, and the tool list contains `GetRandomNumber`, `Echo`, and `GetTimestamp`
- **THEN** the strategy returns only `GetRandomNumber` and `Echo`

#### Scenario: Authenticated user has no tool claims
- **WHEN** an authenticated user has no claims with the `mcp.tool.` prefix, and the tool list contains 5 tools
- **THEN** the strategy returns an empty list

#### Scenario: User is not authenticated
- **WHEN** the `HttpContext.User.Identity.IsAuthenticated` is `false`
- **THEN** the strategy returns all tools unchanged (no-op)

#### Scenario: Claim value is not "true"
- **WHEN** an authenticated user has claim `mcp.tool.Echo` with value `"false"`
- **THEN** `Echo` is excluded from the filtered results

### Requirement: AppSettings tool filtering strategy
The system SHALL provide a built-in `AppSettingsToolFilteringStrategy` that implements `IToolFilteringStrategy` and filters tools based on the configuration key `Mcp:AllowedTools`, which is a JSON array of tool name strings. A tool is included if and only if its name appears in the `Mcp:AllowedTools` array. When the configuration key is missing or empty (null or zero-length array), the strategy returns all tools unchanged.

#### Scenario: AllowedTools contains a subset of tool names
- **WHEN** `Mcp:AllowedTools` is `["GetRandomNumber", "Echo"]`, and the tool list contains `GetRandomNumber`, `Echo`, `GetTimestamp`, `ListUsers`, `GetServerStats`
- **THEN** the strategy returns only `GetRandomNumber` and `Echo`

#### Scenario: AllowedTools is empty
- **WHEN** `Mcp:AllowedTools` is `[]` and the tool list contains 5 tools
- **THEN** the strategy returns all 5 tools unchanged (no-op)

#### Scenario: AllowedTools configuration key is missing
- **WHEN** the configuration has no `Mcp:AllowedTools` section and the tool list contains 5 tools
- **THEN** the strategy returns all 5 tools unchanged (no-op)

#### Scenario: AllowedTools contains a tool name not in the available tools
- **WHEN** `Mcp:AllowedTools` is `["NonExistentTool"]` and the tool list contains `GetRandomNumber` and `Echo`
- **THEN** the strategy returns an empty list (the allowed tool does not exist on the server)

### Requirement: Strategy pipeline applies to both ListTools and CallTool
The system SHALL resolve all registered `IToolFilteringStrategy` implementations from the request's service provider and apply them in registration order on every `ListTools` and `CallTool` MCP request. On `ListTools`, strategies SHALL filter the returned tool list (AND semantics: a tool appears only if it passes ALL strategies). On `CallTool`, the system SHALL extract the target tool name from the request, evaluate it against all registered strategies (AND semantics), and return an error response if any strategy excludes it.

#### Scenario: ListTools — multiple strategies all allow a tool
- **WHEN** both the OAuth Claims strategy and the AppSettings strategy include `GetRandomNumber`, and no other strategies are registered
- **THEN** `GetRandomNumber` appears in the final `ListTools` response

#### Scenario: ListTools — one strategy blocks a tool
- **WHEN** the OAuth Claims strategy includes `GetRandomNumber` and `Echo`, but the AppSettings strategy only allows `GetRandomNumber`
- **THEN** only `GetRandomNumber` appears in the final `ListTools` response (Echo is blocked by AppSettings)

#### Scenario: ListTools — no strategies registered
- **WHEN** no `IToolFilteringStrategy` implementations are registered in DI
- **THEN** `ListTools` returns all available tools unchanged

#### Scenario: ListTools — custom third-party strategy is applied
- **WHEN** a custom `IToolFilteringStrategy` registered in DI excludes `GetTimestamp`, and built-in strategies allow all tools
- **THEN** `GetTimestamp` is excluded from the final `ListTools` response

#### Scenario: CallTool — tool is allowed by all strategies
- **WHEN** a `CallTool` request targets `GetRandomNumber`, and all strategies allow `GetRandomNumber`
- **THEN** the tool executes normally and returns its result

#### Scenario: CallTool — tool is blocked by a strategy
- **WHEN** a `CallTool` request targets `GetTimestamp`, but the AppSettings strategy excludes `GetTimestamp`
- **THEN** the server returns a `CallToolResult` with `IsError = true` and a message indicating the tool is not authorized

### Requirement: Tool filtering registration API
The system SHALL provide an `AddToolFiltering` extension method on `IServiceCollection` that registers the two built-in strategies (`OAuthClaimsToolFilteringStrategy` and `AppSettingsToolFilteringStrategy`) as singleton `IToolFilteringStrategy` implementations, and wires both an `AddListToolsFilter` and an `AddCallToolFilter` into the MCP server builder that apply all registered strategies to `ListTools` responses (filtering) and `CallTool` requests (enforcement), respectively.

#### Scenario: AddToolFiltering registers built-in strategies
- **WHEN** `AddToolFiltering` is called on the service collection
- **THEN** two `IToolFilteringStrategy` service descriptors are registered (OAuthClaims and AppSettings implementations)

#### Scenario: ListTools filter is wired
- **WHEN** the application is built with `AddToolFiltering` called
- **THEN** a `ListTools` MCP request triggers the strategy pipeline before returning the tool list to the client

#### Scenario: CallTool filter is wired
- **WHEN** the application is built with `AddToolFiltering` called
- **THEN** a `CallTool` MCP request triggers the strategy pipeline and blocks invocation if the target tool is not authorized by all strategies

#### Scenario: Filtering enforces at both visibility and invocation levels
- **WHEN** a tool is excluded by a filtering strategy
- **THEN** the tool is hidden from `ListTools` results AND blocked when invoked directly via `CallTool`

### Requirement: Third-party strategy extensibility
The system SHALL allow consumers to register additional `IToolFilteringStrategy` implementations in DI (e.g., via `services.AddSingleton<IToolFilteringStrategy, CustomStrategy>()`), and those implementations SHALL participate in the strategy pipeline alongside the built-in strategies.

#### Scenario: Custom strategy registered after AddToolFiltering
- **WHEN** a consumer registers `services.AddSingleton<IToolFilteringStrategy, CustomStrategy>()` after calling `AddToolFiltering`
- **THEN** `CustomStrategy.FilterTools` is invoked during `ListTools` requests, alongside the OAuth Claims and AppSettings strategies
