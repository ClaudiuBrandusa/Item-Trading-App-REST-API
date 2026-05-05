using Application.Behaviors.Identity.ListUsers;
using Item_Trading_App_Contracts.Responses.Identity;
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
            .MapWith(str => new UsernameSuccessResponse { UserId = MapContext.Current!.Parameters[nameof(UsernameSuccessResponse.UserId)].ToString(), Username = str });
    
        config.ForType<string, ListUsersQuery>()
            .MapWith(str => new ListUsersQuery { SearchString = str, UserId = MapContext.Current!.Parameters![nameof(ListUsersQuery.UserId)]!.ToString()! });
    
        config.ForType<string, AuthenticationFailedResponse>()
            .MapWith(value => new AuthenticationFailedResponse
            {
                Errors = new string[] { value }
            });
    } 
}
