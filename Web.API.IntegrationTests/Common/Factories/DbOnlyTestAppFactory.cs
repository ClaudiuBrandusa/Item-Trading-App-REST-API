using Application.Services.Cache;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Testcontainers.MsSql;
using Web.API.IntegrationTests.Common.Auth;

namespace Web.API.IntegrationTests.Common.Factories;

public class DbOnlyTestAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:latest")
            .WithPassword("YourStrong!Passw0rd")
            .Build();

    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();

        using var conn = new SqlConnection(_dbContainer.GetConnectionString());
        
        await conn.OpenAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await _dbContainer.DisposeAsync().AsTask();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICacheService>();

            var cacheServiceMock = new Mock<ICacheService>();

            services.AddSingleton<ICacheService>((_) => cacheServiceMock.Object);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                options.DefaultChallengeScheme = TestAuthHandler.Scheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });

            services.AddAuthorization();

            services.RemoveAll<IDbContextFactory<DatabaseContext>>();
            services.RemoveAll<DbContextOptions<DatabaseContext>>();
            
            services.AddDbContextFactory<DatabaseContext>(options =>
            options
                .UseSqlServer(_dbContainer.GetConnectionString())
                .ConfigureWarnings(x => x.Ignore(SqlServerEventId.SavepointsDisabledBecauseOfMARS))
            );
        });
    }
}
