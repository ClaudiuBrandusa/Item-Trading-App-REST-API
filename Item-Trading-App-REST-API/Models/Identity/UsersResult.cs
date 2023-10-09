using Item_Trading_App_REST_API.Models.Base;
using System.Collections.Generic;

namespace Item_Trading_App_REST_API.Models.Identity;

public record UsersResult : BaseResult
{
    public IEnumerable<string> UsersId { get; set; }
}
