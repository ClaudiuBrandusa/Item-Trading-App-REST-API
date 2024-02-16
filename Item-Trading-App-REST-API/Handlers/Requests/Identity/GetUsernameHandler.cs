using Item_Trading_App_REST_API.Resources.Queries.Identity;
using Item_Trading_App_REST_API.Services.Identity;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Identity;

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
