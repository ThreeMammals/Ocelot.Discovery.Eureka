using Newtonsoft.Json;
using Steeltoe.Common.Discovery;

namespace Ocelot.Discovery.Eureka.Acceptance;

public class FakeEurekaService : IServiceInstance
{
    public FakeEurekaService(string serviceId, string host, int port, bool isSecure, Uri uri, IDictionary<string, string> metadata)
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
    public IReadOnlyDictionary<string, string> Metadata { get; }

    public string InstanceId => ServiceId;
    public Uri NonSecureUri => Uri;
    public Uri SecureUri => Uri;
}

#pragma warning disable IDE1006 // Naming Styles
public class Port
{
    [JsonProperty("$")]
    public int value { get; set; }

    [JsonProperty("@enabled")]
    public string enabled { get; set; }
}
public class SecurePort
{
    [JsonProperty("$")]
    public int value { get; set; }

    [JsonProperty("@enabled")]
    public string enabled { get; set; }
}
public class DataCenterInfo
{
    [JsonProperty("@class")]
    public string value { get; set; }
    public string name { get; set; }
}
public class LeaseInfo
{
    public int renewalIntervalInSecs { get; set; }
    public int durationInSecs { get; set; }
    public long registrationTimestamp { get; set; }
    public long lastRenewalTimestamp { get; set; }
    public int evictionTimestamp { get; set; }
    public long serviceUpTimestamp { get; set; }
}
public class ValueMetadata
{
    [JsonProperty("@class")]
    public string value { get; set; }
}
public class Instance
{
    public string instanceId { get; set; }
    public string hostName { get; set; }
    public string app { get; set; }
    public string ipAddr { get; set; }
    public string status { get; set; }
    public string overriddenstatus { get; set; }
    public Port port { get; set; }
    public SecurePort securePort { get; set; }
    public int countryId { get; set; }
    public DataCenterInfo dataCenterInfo { get; set; }
    public LeaseInfo leaseInfo { get; set; }
    public ValueMetadata metadata { get; set; }
    public string homePageUrl { get; set; }
    public string statusPageUrl { get; set; }
    public string healthCheckUrl { get; set; }
    public string vipAddress { get; set; }
    public string isCoordinatingDiscoveryServer { get; set; }
    public string lastUpdatedTimestamp { get; set; }
    public string lastDirtyTimestamp { get; set; }
    public string actionType { get; set; }
}
public class Application
{
    public string name { get; set; }
    public List<Instance> instance { get; set; }
}
public class Applications
{
    public string versions__delta { get; set; }
    public string apps__hashcode { get; set; }
    public List<Application> application { get; set; }
}
public class EurekaApplications
{
    public Applications applications { get; set; }
}
#pragma warning restore IDE1006 // Naming Styles
