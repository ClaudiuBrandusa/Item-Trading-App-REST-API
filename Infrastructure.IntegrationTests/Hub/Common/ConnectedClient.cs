using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;

namespace Infrastructure.IntegrationTests.Hub.Common;

public class ConnectedClient : IDisposable
{
    public HubConnection Connection { get; set; }

    private TaskCompletionSource ConnectedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Connected { get; init; }

    public string UserId { get; set; }

    public string UserName { get; set; }

    public Dictionary<string, object> ReceivedBag { get; set; } = new();

    private IDisposable? _connectedListener;

    public ConnectedClient(string userId, string userName, string hubEndpoint, TestServer server)
    {
        UserId = userId;
        UserName = userName;
        Connected = ConnectedTaskCompletionSource.Task;
        Connection = CreateHubConnection(userId, userName, hubEndpoint, server);
    }

    public async Task Connect()
    {
        await Connection.StartAsync();
        _connectedListener = Connection.On("connected", () =>
        {
            ConnectedTaskCompletionSource.TrySetResult();
        });

        Connection.Closed += (exception) =>
        {
            if (!ConnectedTaskCompletionSource.Task.IsCompleted)
            {
                if (exception is not null)
                    ConnectedTaskCompletionSource.TrySetException(exception);
                else
                    ConnectedTaskCompletionSource.TrySetCanceled();
            }

            return Task.CompletedTask;
        };
    }

    public IDisposable Listen<T>(string key, Action<T> action)
    {
        return Connection.On<T>(key, action);
    }

    public void Dispose()
    {
        _connectedListener?.Dispose();
        Connection.DisposeAsync();
    }

    public static HubConnection CreateHubConnection(string userId, string userName, string hubEndpoint, TestServer server)
    {
        return new HubConnectionBuilder()
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Trace);
            })
            .WithUrl($"http://localhost/{hubEndpoint}", options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                options.SkipNegotiation = false;
                options.Transports = HttpTransportType.LongPolling;
                options.CloseTimeout = TimeSpan.FromSeconds(10);

                options.Headers["x-user-id"] = userId;
                options.Headers["x-test-user"] = userName;
            })
            .Build();
    }
}
