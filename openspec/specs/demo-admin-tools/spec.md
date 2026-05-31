## ADDED Requirements

### Requirement: ListUsers tool
The system SHALL provide an MCP tool named `ListUsers` that returns a simulated list of demo users. Each user entry MUST include a username and a role.

#### Scenario: Client lists all demo users
- **WHEN** an MCP client invokes the `ListUsers` tool with no arguments
- **THEN** the server returns a list containing at least 3 demo users, each with `username` and `role` fields

### Requirement: GetServerStats tool
The system SHALL provide an MCP tool named `GetServerStats` that returns simulated server statistics including uptime and request count.

#### Scenario: Client requests server statistics
- **WHEN** an MCP client invokes the `GetServerStats` tool with no arguments
- **THEN** the server returns an object with `uptime` (string describing uptime duration) and `requestCount` (integer) fields
