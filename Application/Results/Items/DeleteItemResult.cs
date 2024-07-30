using Application.Models;

namespace Application.Results.Items;

public record DeleteItemResult : Result
{
    public string ItemId { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;
}
