using Application.Models.Identity;
using Application.Services.Identity;
using MediatR;

namespace Application.Behaviors.Identity.RegisterUser;

public class RegisterHandler : IRequestHandler<RegisterCommand, AuthenticationResult>
{
    private readonly IIdentityService _identityService;

    public RegisterHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<AuthenticationResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        return _identityService.RegisterAsync(request);
    }
}
