using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Steeltoe.Common.Discovery;

namespace Ocelot.Discovery.Eureka.UnitTests;

public class EurekaMiddlewareConfigurationTests
{
    [Fact]
    public async Task ShouldNotBuild()
    {
        // Arrange
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider(true);
        var app = new ApplicationBuilder(sp);

        // Act
        var actual = await Assert.ThrowsAsync<NotSupportedException>(
            () => EurekaMiddlewareConfiguration.Get.Invoke(app));

        // Assert
        Assert.Equal("Failed to create the final configuration in UseOcelot() due to a provider type mismatch. You have added Eureka provider services via AddEureka(), but the actual service discovery provider type is unknown. Please review the ServiceDiscoveryProvider section in your global configuration.",
            actual.Message);
    }

    [Fact]
    public void ShouldBuild()
    {
        // Arrange
        var client = new Mock<IDiscoveryClient>();
        var services = new ServiceCollection();
        services.AddSingleton(client.Object);
        services.Configure<FileGlobalConfiguration>(o => o.ServiceDiscoveryProvider.Type = nameof(Eureka));
        var sp = services.BuildServiceProvider(true);
        var app = new ApplicationBuilder(sp);

        // Act
        var provider = EurekaMiddlewareConfiguration.Get.Invoke(app);

        // Assert
        Assert.Equal(TaskStatus.RanToCompletion, provider.Status);
    }
}
