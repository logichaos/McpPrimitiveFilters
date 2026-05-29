## Context

The integration tests `RateLimitingTests` and `McpRateLimitingTests` both use `McpRateLimitingWebApplicationFactory` with `Shared = SharedType.PerTestSession`. This means all test methods within each class share a single server instance and thus share the same rate-limit counter state. Several tests exhaust the fixed-window permit limit (10 requests per 1-second window) and then assert on 429 responses.

Additionally, `ApiBuilder.RateLimiter.cs` contains a `static readonly ConcurrentDictionary<string, DateTimeOffset> s_firstRejections` that tracks the first rejection time per partition key for calculating a meaningful `Retry-After` header. Because this is a `static` field, it leaks state across all `WebApplicationFactory` instances within the same process. When tests run in parallel, the stale timestamps cause the rejection handler to compute a `remaining` of less than 1 second, which `Math.Floor` rounds down to 0 — producing `Retry-After: 0` and causing `retryAfter.IsPositive()` to fail.

## Goals / Non-Goals

**Goals:**
- Eliminate shared rate-limit counter and retry-after state between test methods so each test runs in isolation
- Keep the fix minimal — no behavior or configuration changes to the rate limiter itself

**Non-Goals:**
- Changing the rate-limiting policies, window durations, or permit limits
- Changing test assertions or test logic
- Changing the retry-after calculation formula (still uses `Math.Floor`; the isolation fix eliminates the zero-remaining scenario)

## Decisions

**Decision 1:** Change `Shared = SharedType.PerTestSession` to `Shared = SharedType.None` in both test classes.

**Alternatives considered:**
- `SharedType.PerClass`: Still shares state across tests within a class.
- `[NotInParallel]` / sequential ordering: Partially addresses parallel interference but not sequential window-overlap issues, and slows test execution.
- `SharedType.None`: Each test gets its own server instance with fresh rate-limit state. Slightly slower startup per test but fully isolated.

**Decision 2:** Replace the `static readonly ConcurrentDictionary` with a singleton DI service.

**Alternatives considered:**
- **Reset the dictionary between tests via a public method**: Couples production code to test concerns; fragile.
- **Use `HttpContext.Items`**: Would track per-request instead of per-partition-key, changing the intended behavior of a decreasing retry-after across multiple rejections in the same window.
- **Use `Math.Ceiling` instead of `Math.Floor`**: Hides the symptom but doesn't fix the root cause; could still produce inconsistent behavior when multiple rejections happen near the window boundary.
- **Singleton DI service**: The dictionary lives in the DI container, which is scoped to each `WebApplicationFactory` instance. The `OnRejected` handler resolves it via `HttpContext.RequestServices`. This is the correct architectural fix — shared state is now owned by the server, not the process.

## Risks / Trade-offs

- **[Slightly slower test execution]** → Each test creates its own `WebApplicationFactory`, adding startup overhead. Mitigation: the factory is lightweight; impact is negligible relative to reliable parallel execution.
- **[Production-code change required]** → The `static` field removal touches `ApiBuilder.RateLimiter.cs`. Mitigation: the change is mechanical (extract static field → register as singleton → resolve from DI); no behavior changes.
