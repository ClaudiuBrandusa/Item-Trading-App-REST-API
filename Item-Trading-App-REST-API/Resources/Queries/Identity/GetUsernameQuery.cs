using MediatR;

namespace Item_Trading_App_REST_API.Resources.Queries.Identity;

public record GetUsernameQuery : IRequest<string>
{
    public string UserId { get; set; }
}
