using Application.Models.Base;

namespace Application.Models.Items;

public record DeleteItemResult : BaseResult
{
    public string ItemId { get; set; }

    public string ItemName { get; set; }
}
