using Item_Trading_App_REST_API.Models.Identity;
using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Commands.Identity;
using Item_Trading_App_REST_API.Services.Identity;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Identity;

public class RegisterHandler : HandlerBase, IRequestHandler<RegisterCommand, AuthenticationResult>
{
    public RegisterHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<AuthenticationResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        return Execute<IIdentityService, AuthenticationResult>(async (identityService) =>
            await identityService.RegisterAsync(request)
        );
    }
}
