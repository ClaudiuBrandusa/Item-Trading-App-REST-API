using Microsoft.Extensions.DependencyInjection;

namespace Web.API.IntegrationTests.Common;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RemoveAllImplementing<TInterface>(this IServiceCollection services)
        where TInterface : class
    {
        var iface = typeof(TInterface);

        var toRemove = services
            .Where(d =>
                d.ServiceType == iface ||

                (d.ImplementationType is not null && iface.IsAssignableFrom(d.ImplementationType)) ||

                (d.ImplementationInstance is not null && iface.IsAssignableFrom(d.ImplementationInstance.GetType()))
            )
            .ToList();

        foreach (var d in toRemove)
            services.Remove(d);

        return services;
    }
}
