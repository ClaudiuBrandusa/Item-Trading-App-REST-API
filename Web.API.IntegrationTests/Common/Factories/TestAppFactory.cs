using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DotNet.Testcontainers.Builders;
using Infrastructure.Common.Outbox;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MsSql;
using Quartz;
using Testcontainers.Redis;
using Web.API.IntegrationTests.Common.Auth;
using Infrastructure.BackgroundJobs;

namespace Web.API.IntegrationTests.Common.Factories;

public class TestAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:latest")
            .WithPassword("YourStrong!Passw0rd")
            .Build();

    private readonly RedisContainer _cacheContainer = new RedisBuilder()
        .WithImage("redis:latest")
        .WithPortBinding(6379)
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilExternalTcpPortIsAvailable(6379))
        .WithEnvironment(new Dictionary<string, string>
        {
            { "ALLOW_EMPTY_PASSWORD", "yes" }
        }.AsReadOnly())
        .Build();

    private JwtSecurityTokenHandler _jwtSecuritytokenHandler = new();

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _dbContainer.StartAsync(),
            _cacheContainer.StartAsync()
        );

        using var conn = new SqlConnection(_dbContainer.GetConnectionString());

        await conn.OpenAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync().AsTask();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                options.DefaultChallengeScheme = TestAuthHandler.Scheme;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });

            services.AddAuthorization();

            var quartzDescriptors = services
                .Where(s => s.ServiceType.Namespace?.Contains("Quartz") == true)
                .ToList();

            foreach (var d in quartzDescriptors)
                services.Remove(d);

            services.AddQuartz(q =>
            {
                q.UseInMemoryStore();
                q.UseDefaultThreadPool(tp => tp.MaxConcurrency = 1);

                var jobKey = new JobKey(nameof(ProcessOutboxMessagesJob));

                q.AddJob<ProcessOutboxMessagesJob>(jobKey!, (IJobConfigurator cfg) => cfg
                    .WithIdentity(jobKey)
                    .StoreDurably())
                .AddTrigger(trigger =>
                    trigger.ForJob(jobKey)
                        .StartNow()
                );
            });

            services.AddQuartzHostedService(opt =>
            {
                opt.WaitForJobsToComplete = true;
                opt.StartDelay = TimeSpan.Zero;
            });

            services.RemoveAll<IDbContextFactory<DatabaseContext>>();
            services.RemoveAll<DbContextOptions<DatabaseContext>>();

            services.AddDbContextFactory<DatabaseContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<ConvertDomainEventsToOutboxMessagesInterceptor>();            

                options
                    .UseSqlServer(_dbContainer.GetConnectionString())
                    .AddInterceptors(interceptor)
                    .ConfigureWarnings(x => x.Ignore(SqlServerEventId.SavepointsDisabledBecauseOfMARS));
            });
        }).ConfigureAppConfiguration((context, builder) =>
        {
            var overrides = new Dictionary<string, string?>()
            {
                ["RedisSettings:ConnectionAddress"] = _cacheContainer.GetConnectionString()
            };

            builder.AddInMemoryCollection(overrides);
        });
    }

    public IEnumerable<Claim> GetClaimsFromToken(string token)
    {
        var jsonToken = _jwtSecuritytokenHandler.ReadToken(token);
        var tokenS = jsonToken as JwtSecurityToken;
        
        return tokenS?.Claims ?? Array.Empty<Claim>();
    }

    public string? GetUserIdFromToken(string token)
    {
        var claims = GetClaimsFromToken(token);
        return claims.FirstOrDefault(x => x.Type == "id")?.Value ?? string.Empty;
    }

    public DatabaseContext GetDatabaseContext()
    {
        var dbContextFactory = Services.GetRequiredService<IDbContextFactory<DatabaseContext>>();
        return dbContextFactory.CreateDbContext();
    }
}
