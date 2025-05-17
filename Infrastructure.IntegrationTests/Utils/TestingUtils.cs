using Application.Installers;
using Mapster;
using MapsterMapper;

namespace Infrastructure.IntegrationTests.Utils;
public static class TestingUtils
{
    public static IMapper GetMapper()
    {
        var config = TypeAdapterConfig.GlobalSettings.Clone();
        config.RuleMap.Clear();
        config.Scan(typeof(MapsterInstaller).Assembly);
        return new Mapper(config);
    }
}
