﻿using Item_Trading_App_REST_API.Data;
using Item_Trading_App_REST_API.Installers;
using Item_Trading_App_REST_API.Services.DatabaseContextWrapper;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Item_Trading_App_Tests.Utils;

public static class TestingUtils
{
    public static IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
        .Build();
    }

    public static IDatabaseContextWrapper GetDatabaseContextWrapper(string id)
    {
        var databaseContextWrapperMock = new Mock<IDatabaseContextWrapper>();

        databaseContextWrapperMock.Setup(x => x.ProvideDatabaseContext())
            .Returns(GetDatabaseContext(id));

        databaseContextWrapperMock.Setup(x => x.ProvideDatabaseContextAsync())
            .ReturnsAsync(GetDatabaseContext(id));

        databaseContextWrapperMock.Setup(x => x.Dispose(It.IsAny<DatabaseContext>()))
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

    public static IMapper GetMapper()
    {
        var config = TypeAdapterConfig.GlobalSettings.Clone();
        config.RuleMap.Clear();
        config.Scan(typeof(MapsterInstaller).Assembly);
        return new Mapper(config);
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

    public static TokenValidationParameters GetTokenValidationParameters(string secret)
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            RequireExpirationTime = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    private static DbContextOptionsBuilder<DatabaseContext> GetDatabaseContextOptions(string id)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
        optionsBuilder.UseInMemoryDatabase(id);
        optionsBuilder.EnableSensitiveDataLogging(true);

        return optionsBuilder;
    }

    private static void DoNothing()
    {
    }
}
