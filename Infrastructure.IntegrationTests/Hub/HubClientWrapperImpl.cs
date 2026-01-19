using Infrastructure.Wrappers.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.IntegrationTests.Hub;

public class HubClientWrapperImpl : IHubClientWrapper
{
    private readonly IClientProxy _clientProxy;

    public HubClientWrapperImpl(IClientProxy clientProxy)
    {
        _clientProxy = clientProxy;
    }

    public async Task SendAsync(string method, object argument, CancellationToken cancellationToken = default)
    {
        await _clientProxy.SendAsync(method, argument, cancellationToken);
    }
}
