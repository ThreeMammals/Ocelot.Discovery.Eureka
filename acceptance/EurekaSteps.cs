using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Testing.Steps;

namespace Ocelot.Discovery.Eureka.Acceptance;

public class EurekaSteps : DiscoverySteps
{
    public static readonly FileRoute[] NoRoutes = null;

    public static void WithEureka(IServiceCollection services)
        => services.AddOcelot().AddEureka();

    public override FileConfiguration GivenConfiguration(params FileRoute[] routes)
    {
        var c = base.GivenConfiguration(routes);
        c.GlobalConfiguration.ServiceDiscoveryProvider = new()
        {
            Type = nameof(Eureka),
        };
        return c;
    }
}
