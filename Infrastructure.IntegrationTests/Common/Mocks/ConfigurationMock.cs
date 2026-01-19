using Microsoft.Extensions.Configuration;
using Moq;

namespace Infrastructure.IntegrationTests.Common.Mocks;

public class ConfigurationMock
{
    /*public static IConfiguration GetMockedConfiguration(Dictionary<string, string> configurationValues)
    {
        var configurationMock = new Mock<IConfiguration>();

        foreach (var (key, value) in configurationValues)
        {
            if (key.Contains("ConnectionStrings"))
            {
                configurationMock.Setup(x => x.GetConnectionString(It.IsAny<string>())).ReturnsAsync(x =>
                {
                    return "";
                });
            }
            else
            {
                configurationMock.Setup(c => c[key]).Returns(value);
            }
        }

        return configurationMock.Object;
    }*/

    public static IConfiguration BuildConfiguration(Dictionary<string, string> configurationValues)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues!)
            .Build();

        return config;
    }
}
