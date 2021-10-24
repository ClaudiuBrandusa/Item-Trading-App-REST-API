using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.HostedServices
{
    public abstract class BaseHostedService : IHostedService
    {
        private readonly ILogger<BaseHostedService> logger;
        private Timer timer;
        private TimeSpan step;

        public BaseHostedService(ILogger<BaseHostedService> logger, TimeSpan step)
        {
            this.logger = logger;
            this.step = step;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            timer = new Timer(async x => { await ExecuteAsync(); },
                null,
                TimeSpan.Zero,
                step);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Log("Stopped");

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            timer?.Dispose();
        }

        protected abstract Task ExecuteAsync();

        protected void Log(string message)
        {
            logger.LogInformation("Hosted service: " + message);
        }
    }
}
