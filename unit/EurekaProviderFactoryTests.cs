using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.Builder;
using Steeltoe.Common.Discovery;

namespace Ocelot.Discovery.Eureka.UnitTests;

public class EurekaProviderFactoryTests
{
    private readonly IServiceCollection _services;
    private readonly ServiceProviderConfiguration _defaultConfig;
    private readonly DownstreamRoute _defaultRoute;

    public EurekaProviderFactoryTests()
    {
        _services = new ServiceCollection();

        _defaultConfig = new ServiceProviderConfiguration()
        {
            Type = nameof(Eureka),
            Host = "localhost",
            Port = 8761,
            Token = null,
            ConfigurationKey = null,
            PollingInterval = 30,
            Scheme = Uri.UriSchemeHttp,
        };

        _defaultRoute = new DownstreamRouteBuilder()
            .WithServiceName("MyService")
            .Build();
    }

    [Fact]
    public void CreateProvider_WhenNoDiscoveryClientRegistered_ShouldThrow()
    {
        // Arrange
        var provider = _services.BuildServiceProvider();

        // Act & Assert
        var ex = Assert.Throws<NotSupportedException>(() =>
            EurekaProviderFactory.Get(provider, _defaultConfig, _defaultRoute));

        // Assert
        Assert.Equal(
            "Cannot get an IDiscoveryClient service during CreateProvider operation to instanciate the Eureka provider!",
            ex.Message);
    }

    [Fact]
    public void CreateProvider_WhenMultipleDiscoveryClientsRegistered_ShouldThrow()
    {
        // Arrange
        _services.AddSingleton<IDiscoveryClient>(new Mock<IDiscoveryClient>().Object);
        _services.AddSingleton<IDiscoveryClient>(new Mock<IDiscoveryClient>().Object);
        var provider = _services.BuildServiceProvider();

        // Act & Assert
        var ex = Assert.Throws<NotSupportedException>(() =>
            EurekaProviderFactory.Get(provider, _defaultConfig, _defaultRoute));

        Assert.Equal(
            "CreateProvider operation detected multiple service discovery providers being registered, but the Eureka provider is not supported in this configuration or in a hybrid service discovery setup.",
            ex.Message);
    }

    [Fact]
    public void CreateProvider_WhenDiscoveryClientIsNullButEnumerableIsNotEmpty_ShouldReturnNull()
    {
        // Arrange
        // This edge case is unlikely in real DI, but we test the null check
        var clients = new List<IDiscoveryClient> { null! };
        _services.AddSingleton<IEnumerable<IDiscoveryClient>>(clients);
        var provider = _services.BuildServiceProvider();

        // Act
        var discoveryProvider = EurekaProviderFactory.Get(provider, _defaultConfig, _defaultRoute);

        // Assert
        Assert.Null(discoveryProvider);
    }

    [Fact]
    public void CreateProvider_WhenProviderTypeIsNotEureka_ShouldReturnNull()
    {
        // Arrange
        var clientMock = new Mock<IDiscoveryClient>();
        _services.AddSingleton<IDiscoveryClient>(clientMock.Object);
        var nonEurekaConfig = new ServiceProviderConfiguration()
        {
            Type = "Consul",
        };
        var provider = _services.BuildServiceProvider();

        // Act
        var result = EurekaProviderFactory.Get(provider, nonEurekaConfig, _defaultRoute);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CreateProvider_WhenConditionsAreMet_ShouldReturnEurekaProvider()
    {
        // Arrange
        var clientMock = new Mock<IDiscoveryClient>();
        _services.AddSingleton<IDiscoveryClient>(clientMock.Object);

        var provider = _services.BuildServiceProvider();

        // Act
        var result = EurekaProviderFactory.Get(provider, _defaultConfig, _defaultRoute);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Eureka>(result);
    }

    [Fact]
    public void CreateProvider_WhenMistypedEurekaTypeInConfig_ShouldIgnoreCase()
    {
        // Arrange
        var clientMock = new Mock<IDiscoveryClient>();
        _services.AddSingleton<IDiscoveryClient>(clientMock.Object);
        var lowerCaseConfig = new ServiceProviderConfiguration()
        {
            Type = nameof(Eureka).ToLower(),
        };
        var provider = _services.BuildServiceProvider();

        // Act
        var result = EurekaProviderFactory.Get(provider, lowerCaseConfig, _defaultRoute);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Eureka>(result);
    }

    [Fact]
    public void CreateProvider_WhenDiscoveryClientEnumerableIsNull_ShouldThrow()
    {
        // Arrange - simulate GetRequiredService returning null (uncommon but possible in broken DI)
        var providerMock = new Mock<IServiceProvider>();
        providerMock.Setup(p => p.GetService(typeof(IEnumerable<IDiscoveryClient>)))
                    .Returns((IEnumerable<IDiscoveryClient>)null!);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            EurekaProviderFactory.Get(providerMock.Object, _defaultConfig, _defaultRoute));

        Assert.Equal(
            "No service for type 'System.Collections.Generic.IEnumerable`1[Steeltoe.Common.Discovery.IDiscoveryClient]' has been registered.",
            ex.Message);
    }

    // Helper to create a more realistic Ocelot DownstreamRoute if needed in future tests
    private static DownstreamRoute CreateDownstreamRoute(string serviceName)
    {
        return new DownstreamRouteBuilder()
            .WithServiceName(serviceName)
            .Build();
    }
}
