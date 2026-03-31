using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ocelot.DependencyInjection;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.Testing;
using Steeltoe.Common.Discovery;
using System.Net;
using TestStack.BDDfy;
using TestStack.BDDfy.Xunit;

namespace Ocelot.Discovery.Eureka.Acceptance;

public sealed class EurekaServiceDiscoveryTests : AcceptanceSteps
{
    private readonly List<IServiceInstance> _eurekaInstances;

    public EurekaServiceDiscoveryTests()
    {
        _eurekaInstances = [];
    }

    [BddfyTheory]
    [Trait("Feat", "262")] // https://github.com/ThreeMammals/Ocelot/issues/262
    [Trait("PR", "324")] // https://github.com/ThreeMammals/Ocelot/pull/324
    [InlineData(true)]
    [InlineData(false)]
    public async Task Should_use_eureka_service_discovery_and_make_request(bool dotnetRunningInContainer)
    {
        Environment.SetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER", dotnetRunningInContainer.ToString());
        const string ProductService = "product";
        const int EurekaPort = 8761;
        var port = PortFinder.GetRandomPort();
        var instanceOne = new FakeEurekaService(ProductService, "localhost", port, false,
            new Uri(DownstreamUrl(port)), metadata: new Dictionary<string, string>());
        var route = GivenDefaultRoute(0);
        route.ServiceName = ProductService;
        route.LoadBalancerOptions = new(nameof(LeastConnection));
        var configuration = GivenConfiguration(route);
        configuration.GlobalConfiguration.ServiceDiscoveryProvider = new()
        {
            Type = nameof(Eureka),
        };
        var body = Body();
        this.Given(x => GivenThereIsAServiceRunningOn(port, body))
            .And(x => GivenThereIsAFakeEurekaServiceDiscoveryProvider(EurekaPort))
            .And(x => GivenTheServicesAreRegisteredWithEureka(instanceOne))
            .And(x => GivenThereIsAConfiguration(configuration))
            .And(x => GivenOcelotIsRunning(WithEureka))
            .When(x => WhenIGetUrlOnTheApiGateway("/"))
            .Then(x => ThenTheStatusCodeShouldBe(HttpStatusCode.OK))
            .And(x => ThenTheResponseBodyShouldBe(body))
            .BDDfy();
    }

    private static void WithEureka(IServiceCollection services)
        => services.AddOcelot().AddEureka();

    private void GivenTheServicesAreRegisteredWithEureka(params IServiceInstance[] serviceInstances)
    {
        foreach (var instance in serviceInstances)
        {
            _eurekaInstances.Add(instance);
        }
    }

    private Task MapEurekaService(HttpContext context)
    {
        if (context.Request.Path.Value != "/eureka/apps")
            return Task.CompletedTask;

        var apps = new List<Application>();
        foreach (var service in _eurekaInstances)
        {
            var a = new Application
            {
                name = service.ServiceId,
                instance =
                [
                    new()
                    {
                        instanceId = $"{service.Host}:{service}",
                        hostName = service.Host,
                        app = service.ServiceId,
                        ipAddr = "127.0.0.1",
                        status = "UP",
                        overriddenstatus = "UNKNOWN",
                        port = new Port {value = service.Port, enabled = "true"},
                        securePort = new SecurePort {value = service.Port, enabled = "true"},
                        countryId = 1,
                        dataCenterInfo = new DataCenterInfo {value = "com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo", name = "MyOwn"},
                        leaseInfo = new LeaseInfo
                        {
                            renewalIntervalInSecs = 30,
                            durationInSecs = 90,
                            registrationTimestamp = 1457714988223,
                            lastRenewalTimestamp= 1457716158319,
                            evictionTimestamp = 0,
                            serviceUpTimestamp = 1457714988223,
                        },
                        metadata = new()
                        {
                            value = "java.util.Collections$EmptyMap",
                        },
                        homePageUrl = $"{service.Host}:{service.Port}",
                        statusPageUrl = $"{service.Host}:{service.Port}",
                        healthCheckUrl = $"{service.Host}:{service.Port}",
                        vipAddress = service.ServiceId,
                        isCoordinatingDiscoveryServer = "false",
                        lastUpdatedTimestamp = "1457714988223",
                        lastDirtyTimestamp = "1457714988172",
                        actionType = "ADDED",
                    },
                ],
            };
            apps.Add(a);
        }

        var applications = new EurekaApplications
        {
            applications = new Applications
            {
                application = apps,
                apps__hashcode = "UP_1_",
                versions__delta = "1",
            },
        };
        var json = JsonConvert.SerializeObject(applications);
        context.Response.Headers.Append("Content-Type", "application/json");
        return context.Response.WriteAsync(json);
    }

    private void GivenThereIsAFakeEurekaServiceDiscoveryProvider(int port)
        => handler.GivenThereIsAServiceRunningOn(port, MapEurekaService);

    protected override async Task MapStatus(HttpContext context)
    {
        try
        {
            await base.MapStatus(context);
        }
        catch (Exception exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsync(exception.StackTrace);
        }
    }
}
