using Infrastructure.Extensions;
using Infrastructure.IntegrationTests.Common.Fixtures;
using Infrastructure.Services.DatabaseContextWrapper;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.IntegrationTests.Database;

public class DatabaseSeedingTests : IClassFixture<DatabaseFixture>
{
    private readonly IDatabaseContextWrapper databaseContextWrapper;
    private readonly IServiceProvider serviceProvider;

    public DatabaseSeedingTests(DatabaseFixture fixture)
    {
        databaseContextWrapper = fixture.ServiceProvider.GetRequiredService<IDatabaseContextWrapper>();
        serviceProvider = fixture.ServiceProvider;
    }

    [Fact(DisplayName = "Seed database")]
    public async Task SeedDatabase()
    {
        // Arrange

        var output = new StringWriter();
        var originalOutput = Console.Out;
        Console.SetOut(output);

        var databaseContext = await databaseContextWrapper.ProvideDatabaseContextAsync();

        // Act

        await databaseContext.SeedDatabase(serviceProvider);
        var consoleContent = output.ToString();

        // Assert

        Assert.DoesNotContain("Failed to seed the database", consoleContent);
    }
}
