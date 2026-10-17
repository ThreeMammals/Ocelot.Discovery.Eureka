using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Ocelot.Configuration;
using Ocelot.LoadBalancer.Balancers;
using Ocelot.Logging;
using Ocelot.Requester;
using Steeltoe.Common.Discovery;
using System.Collections.Concurrent;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using _HeaderNames_ = Microsoft.Net.Http.Headers.HeaderNames;

namespace Ocelot.Discovery.Eureka.Acceptance;

public sealed class ServiceDiscoveryTests : EurekaSteps
{
    private readonly List<IServiceInstance> _eurekaInstances = [];
    private static readonly int EurekaPort = 8761;

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
        var port = PortFinder.GetRandomPort();
        var instanceOne = GivenService(port, ProductService);
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

    [Fact]
    [Trait("Bug", "2352")] // https://github.com/ThreeMammals/Ocelot/issues/2352
    [Trait("PR", "2427")] // https://github.com/ThreeMammals/Ocelot/pull/2427
    [Trait("Release", "25.0.2")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.2
    public async Task ShouldRouteRequestWithChromiumClientHintHeaders()
    {
        var ports = PortFinder.GetPorts(2);
        var serviceUrls = ports.Select(DownstreamUrl).ToArray();
        var configuration = GivenDiscoveryConfiguration(NoRoutes, EurekaPort, loadBalancer: nameof(RoundRobin));
        var instances = ports.Select(p => GivenService(p)).ToArray();
        GivenMultipleServiceInstancesAreRunning(serviceUrls);
        GivenThereIsAFakeEurekaServiceDiscoveryProvider(EurekaPort);
        GivenTheServicesAreRegisteredWithEureka(instances);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithEureka);
        GivenIAddAHeader("sec-ch-ua", "\"Microsoft Edge\";v=\"143\", \"Chromium\";v=\"143\", \"Not A(Brand\";v=\"24\"");
        GivenIAddAHeader("sec-ch-ua-mobile", "?0");
        await Task.WhenAll(
            WhenIGetUrlOnTheApiGatewayConcurrently(DynamicRouting.Path(), 2));
        ThenAllStatusCodesShouldBe(HttpStatusCode.OK);
        ThenAllServicesShouldHaveBeenCalledTimes(2);
        ThenServicesShouldHaveBeenCalledTimes(1, 1); // distribution by RoundRobin algorithm
    }

    [Fact]
    [Trait("Bug", "2428")] // https://github.com/ThreeMammals/Ocelot/issues/2428
    [Trait("PR", "2429")] // https://github.com/ThreeMammals/Ocelot/pull/2429
    [Trait("Release", "25.0.2")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.2
    public async Task ShouldForwardEachRequestToItsOwnDownstreamPath()
    {
        var ports = PortFinder.GetPorts(2);
        var serviceUrls = ports.Select(DownstreamUrl).ToArray();
        var configuration = GivenDiscoveryConfiguration(NoRoutes, EurekaPort, loadBalancer: nameof(RoundRobin));
        var instances = ports.Select(p => GivenService(p)).ToArray();
        GivenMultipleServiceInstancesAreRunning(serviceUrls);
        GivenThereIsAFakeEurekaServiceDiscoveryProvider(EurekaPort);
        GivenTheServicesAreRegisteredWithEureka(instances);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithEureka);
        await WhenIGetUrlOnTheApiGateway(Path("/first"));
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseHeaderIs(HeaderNames.Path, "/first");
        await WhenIGetUrlOnTheApiGateway(Path("/second?id=2"));
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseHeaderIs(HeaderNames.Path, "/second?id=2");
        await WhenIPostUrlOnTheApiGateway(Path("/orders?flag&q=a+b"), "{}", MediaTypeNames.Application.Json);
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseHeaderIs(HeaderNames.Path, "/orders?flag&q=a+b");
        await WhenIDeleteUrlOnTheApiGateway(Path("/orders/7?force=true"));
        ThenTheStatusCodeShouldBe(HttpStatusCode.OK);
        ThenTheResponseHeaderIs(HeaderNames.Path, "/orders/7?force=true");
        ThenAllServicesShouldHaveBeenCalledTimes(4);
        ThenServicesShouldHaveBeenCalledTimes(2, 2); // distribution by RoundRobin algorithm
    }
    [Fact]
    [Trait("Bug", "2428")] // https://github.com/ThreeMammals/Ocelot/issues/2428
    [Trait("PR", "2429")] // https://github.com/ThreeMammals/Ocelot/pull/2429
    [Trait("Release", "25.0.2")] // https://github.com/ThreeMammals/Ocelot/releases/tag/25.0.2
    public async Task ShouldUseOneHttpHandlerForAllDownstreamPathsOfService()
    {
        var ports = PortFinder.GetPorts(2);
        var serviceUrls = ports.Select(DownstreamUrl).ToArray();
        var configuration = GivenDiscoveryConfiguration(NoRoutes, EurekaPort, loadBalancer: nameof(RoundRobin));
        var instances = ports.Select(p => GivenService(p)).ToArray();
        var paths = Enumerable.Range(1, 10).Select(i => $"/users/{i}").ToArray();
        var urls = paths.Select(p => Path(p)).ToArray();
        GivenMultipleServiceInstancesAreRunning(serviceUrls);
        GivenThereIsAFakeEurekaServiceDiscoveryProvider(EurekaPort);
        GivenTheServicesAreRegisteredWithEureka(instances);
        GivenThereIsAConfiguration(configuration);
        GivenOcelotIsRunning(WithConsulAndRequesterTesting);
        await Task.WhenAll(
            WhenIGetUrlOnTheApiGatewayConcurrently(urls.Length, urls)); // TODO As the first cold request, it sends 10 requests to the Consul discovery provider to return a service instance.
        ThenAllStatusCodesShouldBe(HttpStatusCode.OK);
        ThenAllServicesShouldHaveBeenCalledTimes(10);
        ThenServicesShouldHaveBeenCalledTimes(5, 5); // distribution by RoundRobin algorithm
        ThenEachResponsePathShouldBe(paths);
        ThenOneHttpHandlerShouldBeCreated(10);

        void WithConsulAndRequesterTesting(IServiceCollection services)
        {
            WithEureka(services);
            services
                .RemoveAll<IMessageInvokerPool>()
                .AddSingleton<IMessageInvokerPool, CountingMessageInvokerPool>();
        }
        void ThenEachResponsePathShouldBe(string[] paths)
        {
            foreach (var (index, response) in Responses)
            {
                response.ShouldNotBeNull().Headers.GetValues(HeaderNames.Path).ShouldHaveSingleItem()
                    .ShouldBe(paths[index], "Should forward the request to its own downstream path");
            }
        }
        void ThenOneHttpHandlerShouldBeCreated(int totalRequests, [CallerMemberName] string serviceName = null)
        {
            var pool = OcelotServices.GetService<IMessageInvokerPool>() as CountingMessageInvokerPool;
            pool.ShouldNotBeNull().CreatedHandlers.Keys.Count(route => route.ServiceName == serviceName)
                .ShouldBe(1, "Should use one HTTP handler for all downstream paths of the service");
            pool.Counter.ShouldBe(totalRequests);
            pool.AllHandlers.ShouldHaveSingleItem();
            var expected = pool.CreatedHandlers.Values.Single();
            pool.AllHandlers.ShouldAllBe(h => Equals(h, expected));
        }
    }

    private void GivenTheServicesAreRegisteredWithEureka(params IServiceInstance[] serviceInstances)
        => _eurekaInstances.AddRange(serviceInstances);

    private static FakeEurekaService GivenService(int port, [CallerMemberName] string serviceName = null)
        => new(serviceName, "localhost", port, false,
                new Uri(DownstreamUrl(port)),
                metadata: new Dictionary<string, string>());

    private Task MapEurekaService(HttpContext context)
    {
        if (context.Request.Path.Value != "/eureka/apps")
            return Task.CompletedTask;

        var apps = new List<Application>()
        {
            new()
            {
                name = _eurekaInstances[0].ServiceId,
                instance = new(_eurekaInstances.Select(CreateInstance)),
            }
        };
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
        context.Response.Headers.Append(_HeaderNames_.ContentType, MediaTypeNames.Application.Json);
        return context.Response.WriteAsync(json, context.RequestAborted);
    }

    private static Instance CreateInstance(IServiceInstance service) => new()
    {
        instanceId = $"{service.ServiceId}:{service.Host}:{service.Port}",
        hostName = service.Host,
        app = service.ServiceId,
        ipAddr = "127.0.0.1",
        status = "UP",
        overriddenstatus = "UNKNOWN",
        port = new Port { value = service.Port, enabled = "true" },
        securePort = new SecurePort { value = service.Port, enabled = false.ToString() },
        countryId = 1,
        dataCenterInfo = new DataCenterInfo { value = "com.netflix.appinfo.InstanceInfo$DefaultDataCenterInfo", name = "MyOwn" },
        leaseInfo = new LeaseInfo
        {
            renewalIntervalInSecs = 30,
            durationInSecs = 90,
            registrationTimestamp = 1457714988223,
            lastRenewalTimestamp = 1457716158319,
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
    };

    private void GivenThereIsAFakeEurekaServiceDiscoveryProvider(int eurekaPort)
        => handler.GivenThereIsAServiceRunningOn(eurekaPort, MapEurekaService);
}

// TODO Move to Ocelot.Testing project
public class CountingMessageInvokerPool : MessageInvokerPool, IMessageInvokerPool
{
    public CountingMessageInvokerPool(IDelegatingHandlerFactory handlerFactory, IOcelotLoggerFactory loggerFactory)
        : base(handlerFactory, loggerFactory) { }

    public volatile int Counter;
    public override HttpMessageInvoker Get(DownstreamRoute downstreamRoute)
    {
        var invoker = base.Get(downstreamRoute);
        _ = Interlocked.Increment(ref Counter);
        return invoker;
    }

    public readonly ConcurrentDictionary<DownstreamRoute, SocketsHttpHandler> CreatedHandlers = [];
    public readonly ConcurrentBag<SocketsHttpHandler> AllHandlers = [];
    protected override SocketsHttpHandler CreateHandler(DownstreamRoute route)
    {
        var handler = base.CreateHandler(route);
        CreatedHandlers[route] = handler;
        AllHandlers.Add(handler);
        return handler;
    }
}
