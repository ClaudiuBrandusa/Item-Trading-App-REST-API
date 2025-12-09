namespace Infrastructure.Wrappers.Hubs;

public interface IHubClientWrapper
{
    Task SendAsync(string method, object argument, CancellationToken cancellationToken = default);
}
