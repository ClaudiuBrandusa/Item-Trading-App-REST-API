using Application.Results.Identity;
using Application.Services.Identity;
using MediatR;

namespace Application.Behaviors.Identity.ListUsers;

public class ListUsersHandler : IRequestHandler<ListUsersQuery, Result<UsersResult>>
{
    private readonly IIdentityService _identityService;

    public ListUsersHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public Task<Result<UsersResult>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        return _identityService.ListUsers(request);
    }
}
