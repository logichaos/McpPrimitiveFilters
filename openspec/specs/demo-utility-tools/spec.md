## ADDED Requirements

### Requirement: GetTimestamp tool
The system SHALL provide an MCP tool named `GetTimestamp` that returns the current UTC date and time as an ISO 8601 formatted string.

#### Scenario: Client requests current timestamp
- **WHEN** an MCP client invokes the `GetTimestamp` tool with no arguments
- **THEN** the server returns the current UTC time formatted as ISO 8601 (e.g., `2026-05-29T14:30:00Z`)

### Requirement: Echo tool
The system SHALL provide an MCP tool named `Echo` that accepts a text string parameter and returns it unchanged.

#### Scenario: Client echoes a message
- **WHEN** an MCP client invokes the `Echo` tool with `message = "Hello, World!"`
- **THEN** the server returns `"Hello, World!"`

#### Scenario: Client echoes an empty string
- **WHEN** an MCP client invokes the `Echo` tool with `message = ""`
- **THEN** the server returns an empty string `""`
