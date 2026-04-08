using Application.Installers;
using Mapster;
using MapsterMapper;

namespace CommonTestUtils.TestAdaptedServices;

public static class MapperUtils
{
    public static IMapper GetMapper()
    {
        var config = TypeAdapterConfig.GlobalSettings.Clone();
        config.RuleMap.Clear();
        config.Scan(typeof(MapsterInstaller).Assembly);
        return new Mapper(config);
    }
}
