using Domain.Entities.Identity;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Installers;

namespace Infrastructure.Installers;

public class DbInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextFactory<DatabaseContext>(options =>
            options
                .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                .ConfigureWarnings(x => x.Ignore(SqlServerEventId.SavepointsDisabledBecauseOfMARS))
        );

        services.AddIdentityCore<User>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<DatabaseContext>();
    }
}
