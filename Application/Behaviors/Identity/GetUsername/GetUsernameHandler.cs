using Application.Services.Identity;
using MediatR;

namespace Application.Behaviors.Identity.GetUsername;

public class GetUsernameHandler : IRequestHandler<GetUsernameQuery, string>
{
    private readonly IIdentityService _identityService;

    public GetUsernameHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<string> Handle(GetUsernameQuery request, CancellationToken cancellationToken)
    {
        return _identityService.GetUsername(request);
    }
}
