using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;
using Steeltoe.Common.Discovery;

namespace Ocelot.Discovery.Eureka;

public class Eureka : IServiceDiscoveryProvider
{
    private readonly string _serviceName;
    private readonly IDiscoveryClient _client;

    public Eureka(string serviceName, IDiscoveryClient client)
    {
        _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<List<Service>> GetAsync()
    {
        var instances = await _client.GetInstancesAsync(_serviceName, default); // TODO Need CancellationToken
        if (instances is null || instances.Count == 0)
            return [];

        var services = instances
            .Select(i => new Service(
                name: i.ServiceId,
                hostAndPort: new(i.Host, i.Port, i.Uri.Scheme),
                id: i.InstanceId,
                version: i.Metadata.GetValueOrDefault("version") ?? i.InstanceId,
                tags: i.Metadata.Select(m => $"{m.Key}:{m.Value}")))
            .ToList();
        return services;
    }
}
