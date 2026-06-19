# Refactoring `ModelContextProtocol.TestOAuthServer` to Use `TestWebApplicationFactory`

## Current State

`ModelContextProtocol.TestOAuthServer` is a standalone ASP.NET Core minimal API
that implements an OAuth 2.0 authorization server for integration testing. It is
currently started manually in tests:

- **`OAuthWebApplicationFactory`** creates a `new Program(kestrelTransport: null)`
  and calls `RunServerAsync()` in `InitializeAsync()`, passing a cancellation token.
- **`OAuthTestBase`** creates a `new Program(null, KestrelInMemoryTransport)` and
  similarly calls `RunServerAsync()`, using the in-memory transport.
- The OAuth server binds to `https://localhost:7029` and runs as a separate
  background task alongside the McpServer test.

## Goal

Refactor `ModelContextProtocol.TestOAuthServer` so it can be started via
`TestWebApplicationFactory<ModelContextProtocol.TestOAuthServer.Program>`, the
same way `McpServer` is started in integration tests. This would:

1. Provide trace correlation between tests and the OAuth server
2. Enable per-test logging of OAuth server output under the correct TUnit test
3. Remove manual lifecycle management (cancellation tokens, `WaitAsync`, etc.)
4. Leverage TUnit's built-in lifecycle hooks for cleanup

## Required Changes

### 1. Add `TUnit.AspNetCore` Package

Add to `ModelContextProtocol.TestOAuthServer.csproj`:

```xml
<PackageReference Include="TUnit.AspNetCore" />
```

The project already targets `net10.0` (has `<TargetFramework>net10.0</TargetFramework>`)
and uses `Microsoft.NET.Sdk.Web`, so `TestWebApplicationFactory<T>` is compatible.

### 2. Refactor `Program.cs` to Expose a `Program` Type

The current `Program` class is `sealed` with an instance-based `RunServerAsync` method.
For `TestWebApplicationFactory<T>` to work, we need a `Program` class that can be
referenced by the factory.

**Option A:** Keep the current `Program` class but add a `Main`-style entry point
that `WebApplicationFactory` can discover. The factory needs a `Program` type with
a `CreateBuilder`-compatible pattern. Since the current `Program` already uses
`WebApplication.CreateEmptyBuilder()`, this should work directly.

**Option B:** Add a `public partial class Program` with the default entry point
pattern, and move the instance-based logic to a separate class.

### 3. Create `OAuthServerWebApplicationFactory`

```csharp
using Microsoft.AspNetCore.Hosting;
using TUnit.AspNetCore;

public class OAuthServerWebApplicationFactory
    : TestWebApplicationFactory<ModelContextProtocol.TestOAuthServer.Program>
{
    public string[] ValidResources { get; set; } =
    [
        "http://localhost:5000",
        "http://localhost:5000/mcp",
        "http://localhost:7071",
        "https://localhost:7072"
    ];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ValidResources", string.Join(";", ValidResources));
    }
}
```

### 4. Update `RunServerAsync` to Read Config from `IWebHostBuilder`

The current `RunServerAsync` has hardcoded configuration (port, URLs, CORS, demo
client, etc.). These should be moved to either:

- `appsettings.json` (overridable per test via `ConfigureAppConfiguration`)
- `IConfiguration` reads within `RunServerAsync`

Specifically:
- Port binding: Move to `KestrelOptions.ListenLocalhost(port)` where port is
  read from configuration (e.g., `"OAuthServer:Port"`).
- CORS origins: Move to `appsettings.json` under `"Cors:AllowedOrigins"`.
- Demo client registration: Move to configuration or `ConfigureTestServices`.
- `ValidResources`: Pass via configuration rather than a mutable property.

### 5. Update Integration Test Factories

The `OAuthWebApplicationFactory` in the integration test project would change from
manually managing the OAuth server to composing two factories:

```csharp
public sealed class OAuthWebApplicationFactory
    : TestWebApplicationFactory<McpServer.Program>, IAsyncInitializer
{
    private readonly OAuthServerWebApplicationFactory _oauthFactory = new();

    public async Task InitializeAsync()
    {
        // Pre-heat the OAuth server (creates and starts it)
        _ = _oauthFactory.Server;
        await Task.CompletedTask;
    }

    // ... rest stays the same
}
```

Or better yet, use TUnit's `[ClassDataSource]` to share the OAuth factory:

```csharp
public sealed class OAuthWebApplicationFactory
    : TestWebApplicationFactory<McpServer.Program>
{
    [ClassDataSource<OAuthServerWebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required OAuthServerWebApplicationFactory OAuthServer { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }
}
```

### 6. Handle Port Management

When both `McpServer` and `TestOAuthServer` start via `TestWebApplicationFactory`,
port conflicts are avoided because `TestWebApplicationFactory` assigns random ports.
However, the McpServer's OAuth configuration (`Development` environment) hardcodes
`https://localhost:7029` as the authority URL. This needs to be made dynamic:

- In `OAuthOptions`, support a placeholder `{OAuthServerPort}` that gets replaced
  at runtime.
- Or configure the McpServer's `JwtBearerOptions.Authority` to point to the
  `OAuthServerWebApplicationFactory.Server.BaseAddress` at test setup time.

## Migration Path

1. **Phase 1:** Add `TUnit.AspNetCore` to `TestOAuthServer.csproj`. Verify it compiles.
2. **Phase 2:** Refactor `Program.RunServerAsync` to use the standard `WebApplication`
   builder pattern with configuration-based settings.
3. **Phase 3:** Create `OAuthServerWebApplicationFactory` and update one test to
   use it as a proof of concept.
4. **Phase 4:** Migrate all OAuth tests to use the new factory.
5. **Phase 5:** Clean up the old manual lifecycle management code.

## Benefits

- **Trace correlation:** Server-side OAuth spans link back to the test that triggered them.
- **Per-test logging:** `ILogger` output from `TestOAuthServer` appears under the
  correct TUnit test output.
- **Automatic cleanup:** `TestWebApplicationFactory` handles `DisposeAsync` lifecycle,
  no more manual `CancellationTokenSource` management.
- **Parallel-safe:** TUnit's test isolation ensures different tests don't interfere
  with each other's OAuth server state.
- **Consistency:** Both `McpServer` and `TestOAuthServer` are started the same way.
