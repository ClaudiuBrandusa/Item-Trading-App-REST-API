using Item_Trading_App_REST_API.Models.Identity;
using Item_Trading_App_REST_API.Resources.Queries.Identity;
using Item_Trading_App_REST_API.Services.Identity;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Requests.Identity;

public class ListUsersHandler : IRequestHandler<ListUsersQuery, UsersResult>
{
    private readonly IIdentityService _identityService;

    public ListUsersHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<UsersResult> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        return _identityService.ListUsers(request);
    }
}
