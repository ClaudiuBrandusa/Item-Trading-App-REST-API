using Application.Models.Base;

namespace Application.Models.Items;

public record ItemsResult : BaseResult
{
    public IEnumerable<string> ItemsId { get; set; }
}
