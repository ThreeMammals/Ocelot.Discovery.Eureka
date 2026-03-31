using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Repository;
using Ocelot.Responses;
using Steeltoe.Common.Discovery;

namespace Ocelot.Discovery.Eureka.UnitTests;

public class EurekaMiddlewareConfigurationTests
{
    [Fact]
    public async Task ShouldNotBuild()
    {
        // Arrange
        var configRepo = new Mock<IInternalConfigurationRepository>();
        configRepo.Setup(x => x.Get())
            .Returns(new OkResponse<IInternalConfiguration>(new InternalConfiguration()));
        var services = new ServiceCollection();
        services.AddSingleton(configRepo.Object);
        var sp = services.BuildServiceProvider(true);

        // Act
        var actual = await Assert.ThrowsAsync<NotSupportedException>(
            () => EurekaMiddlewareConfiguration.Get(new ApplicationBuilder(sp)));

        // Assert
        Assert.Equal("Failed to create the final configuration in UseOcelot() due to a provider type mismatch. You have added Eureka provider services via AddEureka(), but the actual service discovery provider type is unknown. Please review the ServiceDiscoveryProvider section in your global configuration.",
            actual.Message);
    }

    [Fact]
    public void ShouldBuild()
    {
        // Arrange
        var serviceProviderConfig = new ServiceProviderConfigurationBuilder()
            .WithType(nameof(Eureka)).Build();
        var client = new Mock<IDiscoveryClient>();
        var configRepo = new Mock<IInternalConfigurationRepository>();
        configRepo.Setup(x => x.Get())
            .Returns(new OkResponse<IInternalConfiguration>(new InternalConfiguration() { ServiceProviderConfiguration = serviceProviderConfig }));
        var services = new ServiceCollection();
        services.AddSingleton(configRepo.Object);
        services.AddSingleton(client.Object);
        var sp = services.BuildServiceProvider(true);

        // Act
        var provider = EurekaMiddlewareConfiguration.Get(new ApplicationBuilder(sp));

        // Assert
        Assert.Equal(TaskStatus.RanToCompletion, provider.Status);
    }
}
