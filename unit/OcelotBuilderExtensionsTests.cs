using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.ServiceDiscovery;
using Ocelot.Testing;
using Steeltoe.Discovery.Eureka;
using System.Reflection;

namespace Ocelot.Discovery.Eureka.UnitTests;

public sealed class OcelotBuilderExtensionsTests : Unit
{
    private readonly IServiceCollection _services;
    private readonly IConfiguration _configRoot;
    private IOcelotBuilder? _ocelotBuilder;

    public OcelotBuilderExtensionsTests()
    {
        _configRoot = new ConfigurationRoot([]);
        _services = new ServiceCollection();
        _services.AddSingleton(GetHostingEnvironment());
        _services.AddSingleton(_configRoot);
    }

    private static IWebHostEnvironment GetHostingEnvironment()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.Setup(e => e.ApplicationName)
            .Returns(typeof(OcelotBuilderExtensionsTests).GetTypeInfo().Assembly.GetName().Name ?? string.Empty);
        return environment.Object;
    }

    [Fact]
    [Trait("PR", "734")] // https://github.com/ThreeMammals/Ocelot/pull/734
    [Trait("Feat", "324")] // https://github.com/ThreeMammals/Ocelot/pull/324
    [Trait("Feat", "844")] // https://github.com/ThreeMammals/Ocelot/pull/844
    public void AddEureka_NoExceptions_ShouldSetUpEureka()
    {
        // Arrange
        _ocelotBuilder = _services.AddOcelot(_configRoot);

        // Act, Assert
        _ocelotBuilder.AddEureka();
    }

    [Fact]
    [Trait("PR", "734")] // https://github.com/ThreeMammals/Ocelot/pull/734
    [Trait("Feat", "324")] // https://github.com/ThreeMammals/Ocelot/pull/324
    [Trait("Feat", "844")] // https://github.com/ThreeMammals/Ocelot/pull/844
    public void AddEureka_DefaultServices_HappyPath()
    {
        // Arrange, Act
        _ocelotBuilder = _services.AddOcelot(_configRoot).AddEureka();

        // Assert: AddEurekaDiscoveryClient
        var descriptor = _services.SingleOrDefault(Of<EurekaDiscoveryClient>);
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        descriptor = _services.SingleOrDefault(Of<IHealthCheckHandler>); // is EurekaHealthCheckHandler
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);

        // Assert
        descriptor = _services.SingleOrDefault(Of<ServiceDiscoveryFinderDelegate>);
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        descriptor = _services.SingleOrDefault(Of<OcelotMiddlewareConfigurationDelegate>);
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    private static bool Of<TType>(ServiceDescriptor descriptor)
        where TType : class
        => descriptor.ServiceType.Equals(typeof(TType));
}
