using Application.Models.Identity;
using MediatR;

namespace Application.Behaviors.Identity.ListUsers;

public record ListUsersQuery : IRequest<UsersResult>
{
    public string SearchString { get; set; }

    public string UserId { get; set; }
}
