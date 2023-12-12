using Item_Trading_App_Contracts.Responses.Base;
using Mapster;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;

namespace Item_Trading_App_REST_API.MappingConfigs;

public class GeneralMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.ForType<ModelStateDictionary, FailedResponse>()
            .MapWith(dictionary => new FailedResponse
            {
                Errors = dictionary.Values.SelectMany(x => x.Errors.Select(xx => xx.ErrorMessage))
            });
    }
}
