using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Shared.Installers;

public static class InstallerExtensions
{
    public static void InstallServicesInAssembly(this IServiceCollection services, Assembly assembly, IConfiguration configuration)
    {
        var installers = assembly.ExportedTypes.Where(installer => typeof(IInstaller).IsAssignableFrom(installer) && !installer.IsInterface && !installer.IsAbstract).Select(Activator.CreateInstance).Cast<IInstaller>().ToList();

        installers.ForEach(installer => installer.InstallServices(services, configuration));
    }
}
