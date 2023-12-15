using Item_Trading_App_REST_API.Models.Identity;
using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Resources.Queries.Identity;
using Item_Trading_App_REST_API.Services.Identity;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Handlers.Identity;

public class ListUsersHandler : HandlerBase, IRequestHandler<ListUsersQuery, UsersResult>
{
    public ListUsersHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public Task<UsersResult> Handle(ListUsersQuery request, CancellationToken cancellationToken)
    {
        return Execute<IIdentityService, UsersResult>(async (identityService) =>
            await identityService.ListUsers(request)
        );
    }
}
