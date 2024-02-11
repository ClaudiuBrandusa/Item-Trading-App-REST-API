using Item_Trading_App_REST_API.Handlers.Requests.Base;
using Item_Trading_App_REST_API.Models.Identity;
using Item_Trading_App_REST_API.Resources.Commands.Identity;
using Item_Trading_App_REST_API.Services.Identity;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Identity;

public class LoginHandler : HandlerBase, IRequestHandler<LoginCommand, AuthenticationResult>
{
    public LoginHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<AuthenticationResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        return Execute<IIdentityService, AuthenticationResult>(async (identityService) =>
            await identityService.LoginAsync(request)
        );
    }
}
