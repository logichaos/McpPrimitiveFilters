using Microsoft.AspNetCore.Mvc.Testing;
using TUnit.Core.Interfaces;

namespace McpServer.Integration.Tests;

public class WebApplicationFactory : WebApplicationFactory<Program>, IAsyncInitializer
{
    public Task InitializeAsync()
    {
        _ = Server;

        return Task.CompletedTask;
    }
}
