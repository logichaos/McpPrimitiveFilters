## 1. Isolate test factory instances

- [x] 1.1 In `tests/McpServer.Integration.Tests/RateLimitingTests.cs`, change `Shared = SharedType.PerTestSession` to `Shared = SharedType.None` on the `[ClassDataSource]` attribute
- [x] 1.2 In `tests/McpServer.Integration.Tests/McpRateLimitingTests.cs`, change `Shared = SharedType.PerTestSession` to `Shared = SharedType.None` on the `[ClassDataSource]` attribute

## 2. Fix static state leakage in rate limiter

- [x] 2.1 In `src/McpServer/Infrastructure/ApiBuilder.RateLimiter.cs`, remove the `static readonly ConcurrentDictionary` field
- [x] 2.2 Register a `ConcurrentDictionary<string, DateTimeOffset>` as a singleton service in `ConfigureRateLimiter`
- [x] 2.3 Update `CreateOnRejectedHandler` to resolve the dictionary from `HttpContext.RequestServices` instead of using the static field

## 3. Validation

- [x] 3.1 Run `dotnet test tests/McpServer.Integration.Tests --treenode-filter "/*/*/RateLimitingTests/*|/*/*/McpRateLimitingTests/*"` and verify all tests pass consistently across multiple runs
