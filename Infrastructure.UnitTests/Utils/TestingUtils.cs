using Application.Installers;
using Application.Services.Cache;
using Infrastructure.Data;
using Infrastructure.Services.DatabaseContextWrapper;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Infrastructure_IntegrationTests.Utils;

public static class TestingUtils
{
    public static IMapper GetMapper()
    {
        var config = TypeAdapterConfig.GlobalSettings.Clone();
        config.RuleMap.Clear();
        config.Scan(typeof(MapsterInstaller).Assembly);
        return new Mapper(config);
    }

    public static IDatabaseContextWrapper GetDatabaseContextWrapper(string id)
    {
        var databaseContextWrapperMock = new Mock<IDatabaseContextWrapper>();

        databaseContextWrapperMock.Setup(x => x.ProvideDatabaseContext())
            .Returns(GetDatabaseContext(id));

        databaseContextWrapperMock.Setup(x => x.ProvideDatabaseContextAsync())
            .ReturnsAsync(GetDatabaseContext(id));

        databaseContextWrapperMock.Setup(x => x.DisposeDatabaseContext(It.IsAny<DatabaseContext>()))
            .Callback(DoNothing);

        return databaseContextWrapperMock.Object;
    }

    public static DatabaseContext GetDatabaseContext(string id = "")
    {
        if (string.IsNullOrEmpty(id))
            id = Guid.NewGuid().ToString();

        var optionsBuilder = GetDatabaseContextOptions(id);

        return new DatabaseContext(optionsBuilder.Options);
    }

    private static DbContextOptionsBuilder<DatabaseContext> GetDatabaseContextOptions(string id)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
        optionsBuilder.UseInMemoryDatabase(id);
        optionsBuilder.EnableSensitiveDataLogging(true);

        return optionsBuilder;
    }

    public static UserManager<TUser> GetUserManager<TUser>(IUserStore<TUser> store) where TUser : class
    {
        store ??= new Mock<IUserStore<TUser>>().Object;
        var options = new Mock<IOptions<IdentityOptions>>();
        var idOptions = new IdentityOptions();
        idOptions.Lockout.AllowedForNewUsers = false;
        options.Setup(o => o.Value).Returns(idOptions);
        var userValidators = new List<IUserValidator<TUser>>();
        var validator = new Mock<IUserValidator<TUser>>();
        userValidators.Add(validator.Object);
        var pwdValidators = new List<PasswordValidator<TUser>>
        {
            new()
        };
        var userManager = new UserManager<TUser>(store, options.Object, new PasswordHasher<TUser>(),
            userValidators, pwdValidators, new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(), new ServiceCollection().BuildServiceProvider(),
            new Mock<ILogger<UserManager<TUser>>>().Object);
        validator.Setup(v => v.ValidateAsync(userManager, It.IsAny<TUser>()))
            .Returns(Task.FromResult(IdentityResult.Success)).Verifiable();
        return userManager;
    }

    public static Mock<ICacheService> GetCacheServiceMock()
    {
        var cacheServiceMock = new Mock<ICacheService>();

        cacheServiceMock.Setup(service => service.ListWithPrefix<string>(It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(() => new Dictionary<string, string>());

        return cacheServiceMock;
    }

    private static void DoNothing()
    {
    }
}
