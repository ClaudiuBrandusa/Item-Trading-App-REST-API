using Item_Trading_App_Contracts.Responses.Identity;
using Item_Trading_App_REST_API.Models.Identity;
using Mapster;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;

namespace Item_Trading_App_REST_API.MappingConfigs;

public class IdentityMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<ModelStateDictionary, AuthenticationFailedResponse>()
            .MapWith(dictionary => new AuthenticationFailedResponse { Errors = dictionary.Values.SelectMany(x => x.Errors.Select(xx => xx.ErrorMessage)) });

        config.ForType<string, UsernameSuccessResponse>()
            .MapWith(str => new UsernameSuccessResponse { UserId = str, Username = MapContext.Current!.Parameters[nameof(UsernameSuccessResponse.Username)].ToString() });

        config.ForType<string, ListUsers>()
            .MapWith(str => new ListUsers { SearchString = str, UserId = MapContext.Current!.Parameters[nameof(ListUsers.UserId)].ToString() });

    }
}
