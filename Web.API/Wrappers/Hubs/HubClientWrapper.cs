using Infrastructure.Wrappers.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Wrappers.Hubs;

public class HubClientWrapper : IHubClientWrapper
{
    private readonly IClientProxy _clientProxy;

    public HubClientWrapper(IClientProxy clientProxy)
    {
        _clientProxy = clientProxy;
    }

    public async Task SendAsync(string method, object argument, CancellationToken cancellationToken)
    {
        await _clientProxy.SendAsync(method, argument, cancellationToken);
    }
}
