using Application.Results.Identity;
using Application.Services.Identity;
using MediatR;

namespace Application.Behaviors.Identity.RefreshToken;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, AuthenticationResult>
{
    private readonly IIdentityService _identityService;

    public RefreshTokenHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<AuthenticationResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        return _identityService.RefreshTokenAsync(request);
    }
}
