using System.Reflection;
using DotNet.Testcontainers.Builders;
using Infrastructure.Common.DatabaseContextTransaction;
using Infrastructure.Common.ExecutionStrategy;
using Infrastructure.Wrappers.Hubs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace Web.API.IntegrationTests;

public class ProgramTests
{
    [Fact]
    public async Task Initialization_InitializeNewProgramInstance_ShouldInitializeSuccessfully()
    {
        // Arrange

        var expectedRegisteredServices = ListApplicationExpectedDependencies(new());
        expectedRegisteredServices = ListInfrastructureExpectedDependencies(expectedRegisteredServices);
        var interfaces = new List<Type>();
        expectedRegisteredServices = ListAPIExpectedDependencies(expectedRegisteredServices);

        MsSqlContainer sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:latest")
            .WithName("sqlserver")
            .WithPortBinding(1433)
            .WithPassword("!Ab12345")
            .Build();
        
        RedisContainer redisContainer = new RedisBuilder("redis:latest")
            .WithName("redis")
            .WithPortBinding(6379)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilExternalTcpPortIsAvailable(6379))
            .WithEnvironment(new Dictionary<string, string>
            {
                { "ALLOW_EMPTY_PASSWORD", "yes" }
            }.AsReadOnly())
            .Build();
        
        await Task.WhenAll(sqlContainer.StartAsync(), redisContainer.StartAsync());
        
        var disposables = new List<IDisposable>();

        var factory = new WebApplicationFactory<Program>();

        factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration(async (context, builder) =>
            {
                builder.AddInMemoryCollection(new Dictionary<string, string>()
                {
                    ["ConnectionStrings:DefaultConnection"] = sqlContainer.GetConnectionString(),
                    ["RedisSettings:ConnectionAddress"] = redisContainer.GetConnectionString()
                }.AsReadOnly()!);       
            });
        });

        // Act

        var instance = factory.CreateClient();

        // Assert

        var descriptors = GetRegisteredServices(factory.Services);

        Assert.NotEmpty(descriptors);

        Assert.All(expectedRegisteredServices, expectedRegisteredServicePair =>
        {
            var foundInterface = descriptors.FirstOrDefault(x => x.ServiceType == expectedRegisteredServicePair.Key);

            Assert.NotNull(foundInterface);

            Assert.All(expectedRegisteredServicePair.Value, implementation =>
            {
                var foundImplementation = descriptors.FirstOrDefault(x => x.ImplementationType == implementation);
                
                Assert.NotNull(foundImplementation);
            });
        });

        if (sqlContainer is not null)
        {
            await sqlContainer.StopAsync();
            await redisContainer.StopAsync();
        }

        instance.Dispose();
        factory.Server.Dispose();
    }

    private static IEnumerable<ServiceDescriptor> GetRegisteredServices(IServiceProvider serviceProvider)
    {
        var serviceProviderType = serviceProvider.GetType();

        var callSiteFactoryType = serviceProviderType.GetProperty("CallSiteFactory", BindingFlags.NonPublic | BindingFlags.Instance);
        var callSiteFactory = callSiteFactoryType.GetValue(serviceProvider);

        var descriptorsType = callSiteFactory.GetType().GetProperty("Descriptors", BindingFlags.NonPublic | BindingFlags.Instance);
        var descriptors = descriptorsType.GetValue(callSiteFactory) as IEnumerable<ServiceDescriptor>;
    
        return descriptors;
    }

    private static Dictionary<Type, List<Type>> ListExpectedDependencies(Assembly assembly, Dictionary<Type, List<Type>> interfacesAndImplementations, List<Type> excludedTypes = null, string[]? assemblySubstrings = null)
    {
        if (excludedTypes is null)
        {
            excludedTypes = new();
        }

        var query = assembly.GetTypes()
            .Where(type => type.IsInterface);

        if (assemblySubstrings is not null)
            foreach (var assemblySubstring in assemblySubstrings)
            {
                query = query.Where(x => x.AssemblyQualifiedName.Contains(assemblySubstring));
            }

        var interfaces = query.ToArray();
        
        var implementations = new List<Type>();

        var t = typeof(IHubClientWrapper);

        foreach (var @interface in interfacesAndImplementations.Keys)
        {
            if (t == @interface)
            {
                
            }

            var impl = assembly.GetTypes()
                .Where(type =>
                    type != @interface &&
                    type.GetInterfaces()
                        .Contains(@interface)
                )
                .ToArray();

            if (!interfacesAndImplementations.ContainsKey(@interface))
            {
                interfacesAndImplementations.Add(@interface, new());
            }

            if (impl.Length == 0)
            {
                // has no implementation in the current assembly

                continue;
            }

            foreach (var im in impl)
            {                
                if (excludedTypes.Contains(im))
                    continue;

                interfacesAndImplementations[@interface].Add(im);
            }

            implementations.AddRange(impl);
        }

        foreach (var @interface in interfaces)
        {
            if (t == @interface)
            {
                
            }

            if (excludedTypes.Contains(@interface))
                continue;

            var impl = assembly.GetTypes()
                .Where(type =>
                    type != @interface &&
                    type.GetInterfaces()
                        .Contains(@interface)
                )
                .ToArray();

            interfacesAndImplementations.Add(@interface, new());

            if (impl.Length == 0)
            {
                // has no implementation in the current assembly

                continue;
            }

            foreach (var im in impl)
            {                
                if (excludedTypes.Contains(im))
                    continue;

                interfacesAndImplementations[@interface].Add(im);
            }

            implementations.AddRange(impl);
        }

        return interfacesAndImplementations;
    }

    private static Dictionary<Type, List<Type>> ListApplicationExpectedDependencies(Dictionary<Type, List<Type>> interfacesAndImplementations) =>
        ListExpectedDependencies(typeof(Application.DependencyInjection).Assembly, interfacesAndImplementations, null!, [".Services."]);

    private static Dictionary<Type, List<Type>> ListInfrastructureExpectedDependencies(Dictionary<Type, List<Type>> interfacesAndImplementations) =>
        ListExpectedDependencies(typeof(Infrastructure.DependencyInjection).Assembly, interfacesAndImplementations, new List<Type> { typeof(IHubClientWrapper), typeof(IExecutionStrategyWrapper), typeof(IDatabaseContextTransactionWrapper) });

    private static Dictionary<Type, List<Type>> ListAPIExpectedDependencies(Dictionary<Type, List<Type>> interfacesAndImplementations) =>
        ListExpectedDependencies(typeof(Program).Assembly, interfacesAndImplementations, new List<Type> { typeof(IHubClientWrapper) });
}
