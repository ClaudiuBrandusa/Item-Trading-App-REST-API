using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Requests.Base;

public class HandlerBase
{
    protected readonly IServiceProvider _serviceProvider;
    private readonly IMapper _mapper;

    public HandlerBase(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public HandlerBase(IServiceProvider serviceProvider, IMapper mapper)
    {
        _serviceProvider = serviceProvider;
        _mapper = mapper;
    }

    protected ReturnType Map<InputType, ReturnType>(InputType model)
        where InputType : class
        where ReturnType : class
    {
        return _mapper.From(model).AdaptToType<ReturnType>();
    }

    protected async Task Execute<ServiceType>(Func<ServiceType, Task> func) where ServiceType : class
    {
        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetService<ServiceType>();
        await func(service);
    }

    protected async Task<OutputType> Execute<ServiceType, OutputType>(Func<ServiceType, Task<OutputType>> func) where ServiceType : class
    {
        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetService<ServiceType>();
        return await func(service);
    }
}
