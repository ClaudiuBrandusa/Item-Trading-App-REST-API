using Application.Services.Identity;
using MediatR;

namespace Application.Behaviors.Identity.GetUsername;

public class GetUsernamesHandler : IRequestHandler<GetUsernamesQuery, Result<Dictionary<string, string>>>
{
    private readonly IIdentityService _identityService;

    public GetUsernamesHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<Result<Dictionary<string, string>>> Handle(GetUsernamesQuery request, CancellationToken cancellationToken)
    {
        return _identityService.GetUsernames(request, cancellationToken);
    }
}
