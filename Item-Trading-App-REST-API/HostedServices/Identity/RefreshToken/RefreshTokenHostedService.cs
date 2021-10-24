using Item_Trading_App_REST_API.Options;
using Item_Trading_App_REST_API.Services.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.HostedServices.Identity.RefreshToken
{
    public class RefreshTokenHostedService : BaseHostedService, IDisposable
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public RefreshTokenHostedService(ILogger<RefreshTokenHostedService> logger, RefreshTokenSettings refreshTokenSettings, IServiceScopeFactory serviceScopeFactory) : base(logger, refreshTokenSettings.ClearRefreshTokenInterval)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync()
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var refreshTokenService = scope.ServiceProvider.GetService<IRefreshTokenService>();

                await refreshTokenService.ClearRefreshTokensAsync();
            }

        }
    }
}
