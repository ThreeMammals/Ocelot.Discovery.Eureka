using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Middleware;

namespace Ocelot.Discovery.Eureka;

public class EurekaMiddlewareConfiguration
{
    public static OcelotMiddlewareConfigurationDelegate Get { get; } = GetAsync;

    private static Task GetAsync(IApplicationBuilder builder)
    {
        var repo = builder.ApplicationServices.GetService<IInternalConfigurationRepository>();
        var config = repo.Get();
        if (!UsingEureka(config.Data))
        {
            var type = config.Data?.ServiceProviderConfiguration?.Type ?? "unknown";
            throw new NotSupportedException($"Failed to create the final configuration in {nameof(OcelotMiddlewareExtensions.UseOcelot)}() due to a provider type mismatch. You have added {nameof(Eureka)} provider services via {nameof(OcelotBuilderExtensions.AddEureka)}(), but the actual service discovery provider type is {type}. Please review the {nameof(FileGlobalConfiguration.ServiceDiscoveryProvider)} section in your global configuration.");
        }

        return Task.CompletedTask;
    }

    private static bool UsingEureka(IInternalConfiguration configuration)
        => nameof(Eureka).Equals(configuration?.ServiceProviderConfiguration?.Type, StringComparison.OrdinalIgnoreCase);
}
