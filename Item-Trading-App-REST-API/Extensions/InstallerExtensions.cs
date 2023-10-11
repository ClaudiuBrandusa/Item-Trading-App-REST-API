using Item_Trading_App_REST_API.Installers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Item_Trading_App_REST_API.Extensions;

public static class InstallerExtensions
{
    public static void InstallServicesInAssembly(this IServiceCollection services, IConfiguration configuration)
    {
        var installers = typeof(Program).Assembly.ExportedTypes.Where(installer => typeof(IInstaller).IsAssignableFrom(installer) && !installer.IsInterface && !installer.IsAbstract).Select(Activator.CreateInstance).Cast<IInstaller>().ToList();

        installers.ForEach(installer => installer.InstallServices(services, configuration));
    }
}
