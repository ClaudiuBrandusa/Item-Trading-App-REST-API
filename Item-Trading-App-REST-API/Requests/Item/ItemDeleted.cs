using MediatR;
using System.Collections.Generic;

namespace Item_Trading_App_REST_API.Requests.Item;

public record ItemDeleted : IRequest
{
    public List<string> UserIds { get; set; }

    public string ItemId { get; set; }
}
