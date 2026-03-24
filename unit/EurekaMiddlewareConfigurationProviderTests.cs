using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Ocelot.Configuration.Repository;
using Ocelot.Responses;
using Steeltoe.Discovery;

namespace Ocelot.Discovery.Eureka.UnitTests;

public class EurekaMiddlewareConfigurationProviderTests
{
    [Fact]
    public void ShouldNotBuild()
    {
        // Arrange
        var configRepo = new Mock<IInternalConfigurationRepository>();
        configRepo.Setup(x => x.Get())
            .Returns(new OkResponse<IInternalConfiguration>(new InternalConfiguration()));
        var services = new ServiceCollection();
        services.AddSingleton(configRepo.Object);
        var sp = services.BuildServiceProvider(true);

        // Act
        var provider = EurekaMiddlewareConfigurationProvider.Get(new ApplicationBuilder(sp));

        // Assert
        Assert.Equal(TaskStatus.RanToCompletion, provider.Status);
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
        var provider = EurekaMiddlewareConfigurationProvider.Get(new ApplicationBuilder(sp));

        // Assert
        Assert.Equal(TaskStatus.RanToCompletion, provider.Status);
    }
}
