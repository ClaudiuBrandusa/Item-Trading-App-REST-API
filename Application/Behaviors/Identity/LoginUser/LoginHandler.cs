using Application.Models.Identity;
using Application.Services.Identity;
using MediatR;

namespace Application.Behaviors.Identity.LoginUser;

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
