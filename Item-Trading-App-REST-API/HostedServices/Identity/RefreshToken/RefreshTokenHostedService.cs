using Item_Trading_App_REST_API.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.HostedServices.Identity.RefreshToken
{
    public class RefreshTokenHostedService : BaseHostedService, IDisposable
    {
        public RefreshTokenHostedService(ILogger<RefreshTokenHostedService> logger, RefreshTokenSettings refreshTokenSettings) : base(logger, refreshTokenSettings.ClearRefreshTokenInterval) {}

        protected override async Task ExecuteAsync()
        {
            Log("Hello there");
        }
    }
}
