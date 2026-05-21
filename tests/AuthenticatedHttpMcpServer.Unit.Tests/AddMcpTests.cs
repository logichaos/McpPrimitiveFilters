using AuthenticatedHttpMcpServer.Infrastructure;
using AuthenticatedHttpMcpServer.Infrastructure.ToolSelection;
using Microsoft.Extensions.DependencyInjection;

namespace AuthenticatedHttpMcpServer.Unit.Tests;

public class AddMcpTests
{
    private static IServiceCollection CreateServicesWithMcp()
    {
        var services = new ServiceCollection();
        services.AddMcp();
        return services;
    }

    [Test]
    public async Task AddMcp_RegistersMcpToolRegistryAsSingleton()
    {
        var descriptor = CreateServicesWithMcp()
            .FirstOrDefault(sd => sd.ServiceType == typeof(McpToolRegistry));

        await Assert.That(descriptor).IsNotNull();
        await Assert.That(descriptor!.Lifetime).IsEqualTo(ServiceLifetime.Singleton);
    }

    [Test]
    public async Task AddMcp_RegistersToolSelectionStrategyAsSingleton()
    {
        var descriptor = CreateServicesWithMcp()
            .FirstOrDefault(sd => sd.ServiceType == typeof(HttpContextToolSelectionStrategy));

        await Assert.That(descriptor).IsNotNull();
        await Assert.That(descriptor!.Lifetime).IsEqualTo(ServiceLifetime.Singleton);
    }

    [Test]
    public async Task AddMcp_ToolSelectionStrategyImplementationIsScopeStrategy()
    {
        var descriptor = CreateServicesWithMcp()
            .First(sd => sd.ServiceType == typeof(HttpContextToolSelectionStrategy));

        await Assert.That(descriptor.ImplementationType)
            .IsEqualTo(typeof(ScopeToolsClaimsPrincipalToolSelectionStrategy));
    }
}
