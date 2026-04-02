using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ocelot.Configuration.File;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Middleware;

namespace Ocelot.Discovery.Eureka;

public class EurekaMiddlewareConfiguration
{
    public static OcelotMiddlewareConfigurationDelegate Get { get; } = GetAsync;

    private static Task GetAsync(IApplicationBuilder builder)
    {
        var options = builder.ApplicationServices.GetService<IOptions<FileGlobalConfiguration>>();
        var configuration = options?.Value ?? new();
        var type = configuration.ServiceDiscoveryProvider.Type.IfEmpty("unknown");
        if (!nameof(Eureka).Equals(type, StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Failed to create the final configuration in {nameof(OcelotMiddlewareExtensions.UseOcelot)}() due to a provider type mismatch. You have added {nameof(Eureka)} provider services via {nameof(OcelotBuilderExtensions.AddEureka)}(), but the actual service discovery provider type is {type}. Please review the {nameof(FileGlobalConfiguration.ServiceDiscoveryProvider)} section in your global configuration.");
        }

        return Task.CompletedTask;
    }
}
