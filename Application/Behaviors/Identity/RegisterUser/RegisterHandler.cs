using Application.Results.Identity;
using Application.Services.Identity;
using MediatR;

namespace Application.Behaviors.Identity.RegisterUser;

public class RegisterHandler : IRequestHandler<RegisterCommand, Result<AuthenticationResult>>
{
    private readonly IIdentityService _identityService;

    public RegisterHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<Result<AuthenticationResult>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        return _identityService.RegisterAsync(request);
    }
}
