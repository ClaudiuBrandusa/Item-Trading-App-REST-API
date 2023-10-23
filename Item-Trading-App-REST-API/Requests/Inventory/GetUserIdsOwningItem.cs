using MediatR;
using System.Collections.Generic;

namespace Item_Trading_App_REST_API.Requests.Inventory;

public record GetUserIdsOwningItem : IRequest<List<string>>
{
    public string ItemId { get; set; }
}
