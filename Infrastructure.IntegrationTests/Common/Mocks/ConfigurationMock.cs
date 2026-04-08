using Microsoft.Extensions.Configuration;

namespace Infrastructure.IntegrationTests.Common.Mocks;

public class ConfigurationMock
{
    public static IConfiguration BuildConfiguration(Dictionary<string, string> configurationValues)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues!)
            .Build();

        return config;
    }
}
