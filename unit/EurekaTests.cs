using Ocelot.Testing;
using Ocelot.Values;
using Steeltoe.Common.Discovery;

namespace Ocelot.Discovery.Eureka.UnitTests;

public class EurekaServiceDiscoveryProviderTests : Unit
{
    private readonly Eureka _provider;
    private readonly Mock<IDiscoveryClient> _client;
    private readonly string _serviceId;
    private List<Service>? _result;

    public EurekaServiceDiscoveryProviderTests()
    {
        _serviceId = "Laura";
        _client = new Mock<IDiscoveryClient>();
        _provider = new Eureka(_serviceId, _client.Object);
    }

    [Fact]
    public async Task Should_return_empty_services()
    {
        // Arrange, Act
        _result = await _provider.GetAsync();

        // Assert
        Assert.Empty(_result);
    }

    [Fact]
    public async Task Should_return_service_from_client()
    {
        // Arrange
        var instances = new List<IServiceInstance>
        {
            new EurekaService(_serviceId, "somehost", 801, false, new Uri("http://somehost:801"), new Dictionary<string, string?>()),
        };
        _client.Setup(x => x.GetInstancesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(instances);

        // Act
        _result = await _provider.GetAsync();
        Assert.Single(_result);

        // Assert
        _client.Verify(
            x => x.GetInstancesAsync(_serviceId, It.IsAny<CancellationToken>()),
            Times.Once);

        // Assert: Then The Service Is Mapped
        var actual = _result[0];
        Assert.Equal("somehost", actual.HostAndPort.DownstreamHost);
        Assert.Equal(801, actual.HostAndPort.DownstreamPort);
        Assert.Equal(_serviceId, actual.Name);
    }

    [Fact]
    public async Task Should_return_services_from_client()
    {
        // Arrange
        var instances = new List<IServiceInstance>
        {
            new EurekaService(_serviceId, "somehost", 801, false, new Uri("http://somehost:801"), new Dictionary<string, string?>()),
            new EurekaService(_serviceId, "somehost", 801, false, new Uri("http://somehost:801"), new Dictionary<string, string?>()),
        };
        _client.Setup(x => x.GetInstancesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(instances);

        // Act
        _result = await _provider.GetAsync();

        // Assert
        Assert.Equal(2, _result.Count);
        _client.Verify(
            x => x.GetInstancesAsync(_serviceId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

public class EurekaService : IServiceInstance
{
    public EurekaService(string serviceId, string host, int port, bool isSecure, Uri uri, IDictionary<string, string?> metadata)
    {
        ServiceId = serviceId;
        Host = host;
        Port = port;
        IsSecure = isSecure;
        Uri = uri;
        Metadata = metadata.AsReadOnly();
    }

    public string ServiceId { get; }
    public string Host { get; }
    public int Port { get; }
    public bool IsSecure { get; }
    public Uri Uri { get; }

    public string InstanceId => ServiceId;
    public Uri? NonSecureUri => throw new NotImplementedException();
    public Uri? SecureUri => throw new NotImplementedException();
    public IReadOnlyDictionary<string, string?> Metadata { get; }
}
