using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;
using Ocelot.Infrastructure.Extensions;
using Ocelot.Testing.Steps;
using System.Runtime.CompilerServices;

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

    public FileConfiguration GivenDiscoveryConfiguration(FileRoute[] routes, int port,
        string scheme = null, string host = null, string provider = null, string loadBalancer = null)
    {
        routes ??= [];
        var c = GivenConfiguration(routes);
        var sd = c.GlobalConfiguration.ServiceDiscoveryProvider;
        sd.Scheme = scheme ?? Uri.UriSchemeHttp;
        sd.Host = host ?? "localhost";
        sd.Port = port;
        sd.Type = provider ?? nameof(Eureka);

        if (loadBalancer.IsNotEmpty())
            c.GlobalConfiguration.LoadBalancerOptions = new(loadBalancer);

        return c;
    }

    /// <summary>
    /// Dynamic Routing upstream path.
    /// </summary>
    public static string Path(string path = "/", [CallerMemberName] string serviceName = null)
        => DynamicRouting.Path(path, serviceName);
}

internal static class DynamicRouting
{
    /// <summary>
    /// Dynamic Routing upstream path.
    /// </summary>
    public static string Path(string path = "/", [CallerMemberName] string serviceName = null)
        => $"/{serviceName}/{path.TrimStart('/')}"; // first segment is always Service Name
}
