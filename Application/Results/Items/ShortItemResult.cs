using Application.Models;

namespace Application.Results.Items;

public record ShortItemResult : Result
{
    public string ItemId { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;
}
