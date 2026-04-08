using Application.Services.Cache;
using Moq;

namespace CommonTestUtils.MockedServices;

public static class CacheUtils
{
    public static Mock<ICacheService> GetCacheServiceMock()
    {
        var cacheServiceMock = new Mock<ICacheService>();

        cacheServiceMock.Setup(service => service.ListWithPrefix<string>(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(() => new Dictionary<string, string>());

        cacheServiceMock.Setup(service => service.GetCacheValueAsync(It.IsAny<string>()))
            .ReturnsAsync(() => null!);

        return cacheServiceMock;
    }
}
