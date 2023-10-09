﻿using Item_Trading_App_REST_API.Models.Item;
using MediatR;

namespace Item_Trading_App_REST_API.Requests.Item;

public record GetItemQuery : IRequest<FullItemResult>
{
    public string ItemId { get; set; }
}
