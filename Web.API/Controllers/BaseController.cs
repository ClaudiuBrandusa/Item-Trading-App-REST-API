using Application.Extensions;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Application.Models.Common;
using Item_Trading_App_Contracts.Responses.Base;

namespace Item_Trading_App_REST_API.Controllers;

public class BaseController : Controller
{
    protected readonly IMapper _mapper;

    public BaseController(IMapper mapper)
    {
        _mapper = mapper;
    }

    protected string UserId
    {
        get => User.Claims.First(c => Equals(c.Type, "id"))?.Value;
    }

    protected R AdaptToType<T, R>(T request, params (string, object)[] parameters)
    {
        return _mapper.AdaptToType<T, R>(request, parameters);
    }

    protected ObjectResult MapResult<InputType, SucceededType, FailedType>(Result<InputType> result)
        where SucceededType : class
        where FailedType : FailedResponse
    {
        if (result.IsSuccess)
        {
            return Ok(_mapper.From(result.Content).AdaptToType<SucceededType>());
        }

        var response = new FailedResponse { Errors = [result.Error]};

        if (typeof(FailedType) == typeof(FailedResponse))
            return BadRequest(response);

        return BadRequest(_mapper.From(response).AdaptToType<FailedType>());
    }

    protected ObjectResult MapResult<InputType, SucceededType, FailedType>(Result<InputType> result, params (string, object)[] parameters)
        where SucceededType : class
        where FailedType : class
    {
        if (result.IsSuccess)
        {
            var builder = _mapper.From(result.Content!);

            if (parameters is not null)
                foreach (var parameter in parameters)
                {
                    builder = builder.AddParameters(parameter.Item1, parameter.Item2);
                }

            return Ok(builder.AdaptToType<SucceededType>());
        }
        else
        {
            return BadRequest(_mapper.From(result.Error).AdaptToType<FailedType>());
        }
    }
}
