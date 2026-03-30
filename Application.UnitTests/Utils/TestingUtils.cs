using Application.Installers;
using Application.Services.Cache;
using Domain.Repositories;
using Mapster;
using MapsterMapper;

namespace Application_UnitTests.Utils;

public static class TestingUtils
{
    public static IMapper GetMapper()
    {
        var config = TypeAdapterConfig.GlobalSettings.Clone();
        config.RuleMap.Clear();
        config.Scan(typeof(MapsterInstaller).Assembly);
        return new Mapper(config);
    }

    public static Mock<R> CreateRepositoryMock<T, R>(List<T> collection) where R : class, IRepository
    {
        var repositoryMock = new Mock<R>();

        repositoryMock.Setup(repo => repo.AddEntityAsync(It.IsAny<It.IsSubtype<T>>()))
                      .ReturnsAsync(true)
                      .Callback((object item) =>
                      {
                          var entity = (T)item;

                          collection.Add(entity);
                      });

        repositoryMock.Setup(repo => repo.UpdateEntityAsync(It.IsAny<It.IsSubtype<T>>()))
                      .ReturnsAsync(true)
                      .Callback((object item) =>
                       {
                           T entity = (T)item;

                           int index = collection.IndexOf(entity);

                           if (index == -1) return;

                           collection[index] = entity;
                       });

        repositoryMock.Setup(repo => repo.AddOrUpdateEntityAsync(It.IsAny<It.IsSubtype<T>>(), It.IsAny<Func<bool>>()))
                      .ReturnsAsync(true)
                      .Callback((object item, Func<bool> condition) =>
                      {
                          T entity = (T)item;

                          int index = collection.IndexOf(entity);

                          if (index == -1)
                          {
                              collection.Add(entity);
                          }
                          else
                          {
                              collection[index] = entity;
                          }
                      });

        repositoryMock.Setup(repo => repo.RemoveEntityAsync(It.IsAny<It.IsSubtype<T>>()))
                      .ReturnsAsync(true)
                      .Callback((object item) =>
                      {
                          T entity = (T)item;

                          int index = collection.IndexOf(entity);

                          if (index == -1) return;

                          collection.RemoveAt(index);
                      });

        repositoryMock.Setup(repo => repo.SaveChangesAsync())
                      .ReturnsAsync(collection.Count);

        return repositoryMock;
    }

    public static Mock<ICacheService> GetCacheServiceMock()
    {
        var cacheServiceMock = new Mock<ICacheService>();

        cacheServiceMock.Setup(service => service.GetCacheValueAsync(It.IsAny<string>()))
            .ReturnsAsync(() => null!);

        return cacheServiceMock;
    }
}
