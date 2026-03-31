using Application.Results.Identity;
using MediatR;

namespace Application.Behaviors.Identity.ListUsers;

public record ListUsersQuery : IRequest<Result<UsersResult>>
{
    public string SearchString { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
}
