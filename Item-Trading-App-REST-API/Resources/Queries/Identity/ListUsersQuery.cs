using Item_Trading_App_REST_API.Models.Identity;
using MediatR;

namespace Item_Trading_App_REST_API.Resources.Queries.Identity;

public record ListUsersQuery : IRequest<UsersResult>
{
    public string SearchString { get; set; }

    public string UserId { get; set; }
}
