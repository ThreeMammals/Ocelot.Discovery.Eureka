using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;

namespace Ocelot.Discovery.Eureka;

public static class OcelotBuilderExtensions
{
    public static IOcelotBuilder AddEureka(this IOcelotBuilder builder)
    {
        builder.Services
            .AddEurekaDiscoveryClient()
            .AddSingleton(EurekaProviderFactory.Get)
            .AddSingleton(EurekaMiddlewareConfiguration.Get);
        return builder;
    }
}
