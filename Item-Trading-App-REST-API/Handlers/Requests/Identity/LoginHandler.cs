using Item_Trading_App_REST_API.Models.Identity;
using Item_Trading_App_REST_API.Resources.Commands.Identity;
using Item_Trading_App_REST_API.Services.Identity;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Identity;

public class LoginHandler : IRequestHandler<LoginCommand, AuthenticationResult>
{
    private readonly IIdentityService _identityService;

    public LoginHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<AuthenticationResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        return _identityService.LoginAsync(request);
    }
}
