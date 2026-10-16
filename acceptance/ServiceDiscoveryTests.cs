using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Ocelot.LoadBalancer.Balancers;
using Steeltoe.Common.Discovery;

namespace Ocelot.Discovery.Eureka.Acceptance;

public sealed class ServiceDiscoveryTests : EurekaSteps
{
    private readonly List<IServiceInstance> _eurekaInstances = [];

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Feat", "262")] // https://github.com/ThreeMammals/Ocelot/issues/262
    [Trait("PR", "324")] // https://github.com/ThreeMammals/Ocelot/pull/324
    [Trait("Release", "5.5.4")] // https://github.com/ThreeMammals/Ocelot/releases/tag/5.5.4
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
        GivenThereIsAServiceRunningOn(port);
        GivenThereIsAFakeEurekaServiceDiscoveryProvider(EurekaPort);
        GivenTheServicesAreRegisteredWithEureka(instanceOne);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithEureka);
        await WhenIGetUrlOnTheApiGateway("/");
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        await ThenTheResponseBodyAsync();
    }

    private void GivenTheServicesAreRegisteredWithEureka(params IServiceInstance[] serviceInstances)
        => _eurekaInstances.AddRange(serviceInstances);

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
        return context.Response.WriteAsync(json, context.RequestAborted);
    }

    private void GivenThereIsAFakeEurekaServiceDiscoveryProvider(int eurekaPort)
        => handler.GivenThereIsAServiceRunningOn(eurekaPort, MapEurekaService);
}
