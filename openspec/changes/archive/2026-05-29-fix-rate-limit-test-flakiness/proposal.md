## Why

Rate-limit integration tests (`RateLimitingTests` and `McpRateLimitingTests`) are flaky for two reasons. First, they share a single `McpRateLimitingWebApplicationFactory` instance via `SharedType.PerTestSession`, so tests compete for the same rate-limiter counter. Second, the `s_firstRejections` dictionary in `ApiBuilder.RateLimiter.cs` is a `static readonly` field, leaking retry-after state across all server instances in the process — even when each test creates a fresh factory, the static dictionary retains stale timestamps that cause `Retry-After: 0` responses.

## What Changes

- Change `McpRateLimitingWebApplicationFactory` class-data sharing from `SharedType.PerTestSession` to `SharedType.None` in both `RateLimitingTests` and `McpRateLimitingTests`, giving each test method its own isolated server instance
- Replace the `static readonly ConcurrentDictionary` in `ApiBuilder.RateLimiter.cs` with a singleton service registered via DI, so the rejection-state dictionary is scoped to each server instance

## Capabilities

### New Capabilities

<!-- None; this is a test-isolation and production-code fix only. -->

### Modified Capabilities

<!-- None; no spec-level behavior changes. -->

## Impact

- Affected files:
  - `tests/McpServer.Integration.Tests/RateLimitingTests.cs`
  - `tests/McpServer.Integration.Tests/McpRateLimitingTests.cs`
  - `src/McpServer/Infrastructure/ApiBuilder.RateLimiter.cs`
- No API changes, no external dependency changes
