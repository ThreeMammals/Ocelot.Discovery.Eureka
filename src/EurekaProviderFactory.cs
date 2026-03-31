using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.ServiceDiscovery.Providers;
using Steeltoe.Common.Discovery;

namespace Ocelot.Discovery.Eureka;

public static class EurekaProviderFactory
{
    public static ServiceDiscoveryFinderDelegate Get { get; } = CreateProvider;

    private static IServiceDiscoveryProvider CreateProvider(IServiceProvider provider, ServiceProviderConfiguration config, DownstreamRoute route)
    {
        var clients = provider.GetRequiredService<IEnumerable<IDiscoveryClient>>();
        if (clients == null || !clients.Any())
            throw new NotSupportedException($"Cannot get an {nameof(IDiscoveryClient)} service during {nameof(CreateProvider)} operation to instanciate the {nameof(Eureka)} provider!");

        if (clients.TryGetNonEnumeratedCount(out int count) && count > 1)
            throw new NotSupportedException($"{nameof(CreateProvider)} operation detected multiple service discovery providers being registered, but the {nameof(Eureka)} provider is not supported in this configuration or in a hybrid service discovery setup.");

        var client = clients.FirstOrDefault();
        if (client is null || !nameof(Eureka).Equals(config.Type, StringComparison.OrdinalIgnoreCase))
            return null;

        return new Eureka(route.ServiceName, client); // TODO Add caching per (route) service name
    }
}
