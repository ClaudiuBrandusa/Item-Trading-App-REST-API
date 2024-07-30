using Application.Results.Identity;
using Application.Services.Identity;
using MediatR;

namespace Application.Behaviors.Identity.ListUsers;

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
