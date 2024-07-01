using Application.Behaviors.Identity.ListUsers;
using Mapster;

namespace Application.Mapper;

public class IdentityMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<string, ListUsersQuery>()
            .MapWith(str => new ListUsersQuery { SearchString = str, UserId = MapContext.Current!.Parameters[nameof(ListUsersQuery.UserId)].ToString() });
    }
}
