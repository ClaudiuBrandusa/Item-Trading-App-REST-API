using Application.Models.Base;

namespace Application.Models.Identity;

public record UsersResult : BaseResult
{
    public IEnumerable<string> UsersId { get; set; }
}
