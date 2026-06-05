# Refactor to FluentResults — Analysis & Strategy

## Context

This document captures the analysis of replacing exceptions with the
[FluentResults](https://github.com/altmann/FluentResults) monad for business/domain
errors in `src/McpServer/`.  It is written so that another agent (or a future
you) can pick up the work without redoing the investigation.

---

## 1. Current Architecture

```
MCP Client ──(JSON-RPC)──▶ ASP.NET Core ──▶ MCP SDK (ModelContextProtocol)
                                              │
                                              ├── Tools:  RandomNumberTools (5 tools)
                                              └── Resources: DemoResources (5 resources)
```

### Project layout
```
src/McpServer/
  Program.cs
  Infrastructure/
    ApiBuilder.Authentication.cs    — OAuth (Auth0, EntraId, InMemory)
    ApiBuilder.Logging.cs           — Dynamic log level
    ApiBuilder.Maps.cs              — GET / healthcheck
    ApiBuilder.Mcp.cs               — MCP server setup, tool/resource filters
    ApiBuilder.RateLimiter.cs       — Fixed-window rate limiting
    ApiBuilder.ResourceFiltering.cs — DI registration for resource filtering
    ApiBuilder.ToolFiltering.cs     — DI registration for tool filtering
    DynamicLogLevelService.cs
    OAuth/                          — OAuthSchemeConfigurator + 3 providers
    ToolFiltering/                  — 2 strategy interfaces + 4 implementations
  Tools/RandomNumberTools.cs
  Resources/DemoResources.cs
```

**NuGet packages** (from `Directory.Packages.props`):
- `ModelContextProtocol.AspNetCore` 1.3.0  (the MCP SDK)
- `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.8

Test project uses `TUnit` + `FakeItEasy`.

---

## 2. Full Exception Inventory

Every `throw` statement in `src/McpServer/`, categorized:

### 2.1 Startup / Configuration Validation (keep as exceptions ✅)

| File | Exception | Trigger |
|---|---|---|
| `ApiBuilder.Authentication.cs:95` | `InvalidOperationException` | Unknown OAuth provider type in config |
| `ApiBuilder.Authentication.cs:143` | `InvalidOperationException` | `DefaultScheme` not set |
| `ApiBuilder.Authentication.cs:149` | `InvalidOperationException` | `DefaultScheme` is not an enabled scheme |
| `ApiBuilder.RateLimiter.cs:58` | `InvalidOperationException` | `FixedWindowRateLimit` not configured |
| `ApiBuilder.RateLimiter.cs:61` | `InvalidOperationException` | `McpWindowRateLimit` not configured |
| `InMemoryOAuthConfigurator.cs:16` | `InvalidOperationException` | Missing `AuthorityUrl` |
| `EntraIdOAuthConfigurator.cs:15` | `InvalidOperationException` | Missing `TenantId` |
| `Auth0OAuthConfigurator.cs:15` | `InvalidOperationException` | Missing `Domain` |

**Verdict:** These are **fail-fast startup errors**.  The application cannot
recover from misconfiguration — throwing during `ConfigureServices` is the
idiomatic .NET approach.  Do **not** replace these with Result types.

### 2.2 MCP Protocol Violations (keep as exceptions ✅)

| File | Exception | Trigger |
|---|---|---|
| `ApiBuilder.Mcp.cs:168` | `McpProtocolException` | Missing `level` param in logging handler |

**Verdict:**  The MCP SDK contract requires `McpProtocolException` / `McpException`
for protocol-level errors.  The SDK translates these into JSON-RPC error
responses.  Do **not** replace.

### 2.3 Business / Domain Error (candidate for Result ⚠️)

| File | Exception | Trigger |
|---|---|---|
| `DemoResources.cs:113` | `McpException` | Invalid `format` parameter for `time://{format}` |

```csharp
// Current code (DemoResources.cs, line ~113)
_ => throw new McpException(
    $"Unknown format '{format}'. Supported: iso, unix, rfc, ticks.")
```

This is a **user-input validation error**, not an infrastructure failure.
It is the textbook case for a `Result` type:
- The caller is the MCP client (a user), not internal code.
- You want to communicate *which* values are valid (structured metadata).
- There is no stack-unwinding benefit — this is a known, expected failure mode.

---

## 3. The MCP SDK Integration Constraint

The MCP SDK (`ModelContextProtocol.AspNetCore`) expects tools and resources
to either:

1. **Return a value directly** → SDK wraps it in a success JSON-RPC response.
2. **Throw `McpException` or `McpProtocolException`** → SDK translates to error response.

The SDK does **not** natively understand `Result<T>` return types.  If a tool
returns `Result<int>`, the SDK will serialize the entire Result object (including
`IsSuccess`, `Errors`, etc.) into the response — the client won't receive what
you intend.

### The dispatch chain

```
MCP SDK invokes tool via [McpServerTool] attribute
  ↓
Tool method returns T
  ↓
SDK wraps in CallToolResponse / ReadResourceResponse
  ↓
JSON-RPC serialized to client
```

Any `Result<T>` integration must sit **between** your business logic and the
MCP SDK surface.

---

## 4. Three Integration Strategies

### Strategy A — Full Result Pipeline (max purity, high complexity)

Every tool/resource returns `Result<T>`.  A custom `CallToolFilter` shim
inspects the return type and converts failures to `McpException`.

```
Tool returns Result<T>
  ↓
CallToolFilter shim inspects Result
  ├── IsSuccess → unwrap .Value, return success
  └── IsFailed  → throw McpException(errors.ToMessage())
```

**Implementation sketch:**

```csharp
// 1. All tools return Result<T>
[McpServerTool]
public Result<int> GetRandomNumber(int min, int max)
{
    if (min >= max)
        return Result.Fail<int>("Minimum must be less than maximum.");
    return Random.Shared.Next(min, max);
}

// 2. Filter in ApiBuilder.Mcp.cs
filters.AddCallToolFilter(next => async (context, ct) =>
{
    var response = await next(context, ct);

    // Inspect the raw response — if the tool method returned a Result,
    // convert it.  This requires a convention or reflection.
    // Problem: the SDK already serialized the Result<T> at this point.
    // You'd need to intercept BEFORE the SDK serializes.

    return response;
});
```

**Blockers:**
- The MCP SDK's `CallToolFilter` runs **after** the tool has executed and the
  return value has been captured by the SDK.  You cannot retroactively unwrap
  a `Result<T>` from inside a filter — the serialization already happened.
- To intercept before, you'd need to replace the SDK's tool invocation
  mechanism (complex, fragile).

**Verdict:** ❌ Not practical with the current MCP SDK architecture.

---

### Strategy B — Internal Result layer, throw at the boundary (pragmatic ✅)

Business logic uses `Result<T>` internally.  The public `[McpServerTool]`
method is a thin adapter that calls the pure logic and converts Result to
either a return value or an exception.

```
Internal pure method returns Result<T>
  ↓
Public [McpServerTool] method calls .Match()
  ├── success → return value
  └── failure → throw McpException(error metadata)
```

**Implementation sketch:**

```csharp
// --- Internal business layer (pure, testable, no exceptions) ---

public static class TimeResourceLogic
{
    private static readonly string[] ValidFormats = ["iso", "unix", "rfc", "ticks"];

    public static Result<string> FormatCurrentTime(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
            return Result.Fail<string>("Format is required.");

        var normalized = format.ToLowerInvariant();
        var utcNow = DateTimeOffset.UtcNow;

        return normalized switch
        {
            "iso"   => utcNow.ToString("O"),
            "unix"  => utcNow.ToUnixTimeSeconds().ToString(),
            "rfc"   => utcNow.ToString("R"),
            "ticks" => utcNow.Ticks.ToString(),
            _       => Result.Fail<string>($"Unknown format '{format}'.")
                            .WithMetadata("ValidFormats", ValidFormats)
        };
    }
}

// --- MCP surface layer (thin adapter) ---

public TextResourceContents GetCurrentTime(
    RequestContext<ReadResourceRequestParams> requestContext, string format)
{
    var result = TimeResourceLogic.FormatCurrentTime(format);

    if (result.IsSuccess)
    {
        return new TextResourceContents
        {
            Uri = requestContext.Params.Uri,
            MimeType = "text/plain",
            Text = result.Value
        };
    }

    // Convert structured errors to MCP exception with metadata
    throw new McpException(
        string.Join("; ", result.Errors.Select(e => e.Message)));
}
```

**Pros:**
- Business logic is pure and testable without mocking exception flow.
- Errors carry structured metadata (`ValidFormats`).
- MCP surface is unchanged — SDK compatibility preserved.
- Gradual adoption — refactor one tool/resource at a time.

**Cons:**
- Still throws at the boundary (but only once, at the outermost adapter).
- Two layers to maintain (but the adapter is trivial boilerplate).

---

### Strategy C — Typed exceptions with metadata (minimal change)

Keep the exception flow but replace raw `McpException("message")` with
typed exceptions that carry structured metadata.

```csharp
public class McpValidationException : McpException
{
    public string ParameterName { get; }
    public IReadOnlyList<string> ValidValues { get; }

    public McpValidationException(string parameterName, string[] validValues)
        : base($"Invalid '{parameterName}'. Valid: {string.Join(", ", validValues)}")
    {
        ParameterName = parameterName;
        ValidValues = validValues;
    }
}
```

**Pros:** Zero friction with SDK.  **Cons:** Doesn't satisfy the "no exceptions
for business errors" goal.

---

## 5. Recommended Approach: Strategy B

For this codebase, **Strategy B** is the right trade-off between purity and
practical SDK compatibility.

### NuGet Package

```
dotnet add package FluentResults --version 3.16.0
```

Or add to `Directory.Packages.props`:
```xml
<PackageVersion Include="FluentResults" Version="3.16.0" />
```

### Step-by-step implementation plan

1. **Add `FluentResults` package** to `src/McpServer/` and test project.

2. **Create a `Domain/` folder** (or `Logic/`) with pure, Result-returning classes:
   - `TimeResourceLogic.FormatCurrentTime(string format) → Result<string>`
   - `WeatherLogic.GetWeather(string city) → Result<WeatherInfo>`
   - `RandomNumberLogic.Generate(int min, int max) → Result<int>`
   - etc.

3. **Refactor existing `[McpServerTool]` methods** to delegating thin adapters:
   ```csharp
   public TextResourceContents GetCurrentTime(...)
       => TimeResourceLogic.FormatCurrentTime(format)
           .Match(
               success: text => new TextResourceContents { ... Text = text },
               failure: errors => throw new McpException(
                   string.Join("; ", errors.Select(e => e.Message))));
   ```

4. **Add unit tests** for the `Domain/` classes — the key benefit is that
   these tests don't need ASP.NET Core fixtures or MCP SDK setup.

5. **(Optional) Add a `ResultExtensions.cs` helper:**
   ```csharp
   public static class ResultExtensions
   {
       public static T UnwrapOrThrowMcp<T>(this Result<T> result)
       {
           if (result.IsSuccess) return result.Value;
           throw new McpException(
               string.Join("; ", result.Errors.Select(e => e.Message)));
       }
   }
   ```
   Then at the boundary: `return logicResult.UnwrapOrThrowMcp();`

### Where to START

The lowest-hanging fruit — pick these first:

| Priority | File | Current behavior |
|---|---|---|
| 1 | `DemoResources.GetCurrentTime` | Only business exception in the codebase |
| 2 | `RandomNumberTools.GetRandomNumber` | Could validate min < max (no exception today, but should have one) |
| 3 | `DemoResources.GetWeather` | Could validate city is not empty |

### Testing approach

The pure logic layer enables straightforward testing:

```csharp
[Test]
public async Task FormatCurrentTime_InvalidFormat_ReturnsFailure()
{
    var result = TimeResourceLogic.FormatCurrentTime("bogus");

    await Assert.That(result.IsFailed).IsTrue();
    await Assert.That(result.Errors).HasCount().EqualTo(1);
    await Assert.That(result.Errors[0].Message)
        .Contains("bogus");
    await Assert.That(result.Errors[0].Metadata["ValidFormats"])
        .IsEqualTo(new[] { "iso", "unix", "rfc", "ticks" });
}
```

No `WebApplicationFactory`, no HTTP pipeline, no JWT tokens needed.

---

## 6. What NOT to change

| Code | Reason |
|---|---|
| All `InvalidOperationException` throws in `ApiBuilder.*` | Startup config validation — fail-fast is correct |
| `McpProtocolException` in `SetLoggingLevel` handler | MCP SDK contract |
| All `OAuth*Configurator` throws | Startup validation |
| Tool/Resource filtering strategies | They return `IEnumerable<string>`, not exceptions — already composable |
| Rate limiter rejection handler | Returns HTTP 429 with headers, no exceptions |

---

## 7. FluentResults API Quick Reference

```csharp
using FluentResults;

// Success
Result<int> ok = Result.Ok(42);
Result success = Result.Ok();

// Failure
Result<int> fail = Result.Fail<int>("Invalid input.");
Result<int> multi = Result.Fail<int>()
    .WithError("Error 1")
    .WithError(new ValidationError("Field X is required"));

// Metadata
Result.Fail("Bad format")
    .WithMetadata("ValidFormats", new[] { "iso", "unix" });

// Custom error types
public class ValidationError : Error
{
    public string Field { get; }
    public ValidationError(string field, string message) : base(message)
    {
        Field = field;
        Metadata["Field"] = field;
    }
}

// Matching (no native .Match — use helpers or if/else)
if (result.IsSuccess) { var val = result.Value; }
else { var errs = result.Errors; }

// Chaining
Result<string> Transform(Result<int> r) =>
    r.Bind(v => Result.Ok(v.ToString()));
```

---

## 8. Summary

| Question | Answer |
|---|---|
| Are exceptions being abused today? | No — 10/11 throws are correct startup validation |
| The one business exception? | `DemoResources.GetCurrentTime` for invalid format |
| Can we go full Result<T> end-to-end? | Not without fighting the MCP SDK |
| Best approach? | **Strategy B** — pure internal logic returning Result, thin MCP adapter |
| Effort estimate? | ~2–4 hours for full conversion |
| Risk? | Low — additive change, existing tests still pass |

**Next step:** Add `FluentResults` NuGet, extract pure logic classes into a
`Domain/` folder, refactor the one business exception, and add tests.
