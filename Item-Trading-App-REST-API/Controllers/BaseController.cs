using Item_Trading_App_REST_API.Extensions;
using Item_Trading_App_REST_API.Models.Base;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

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

    protected ObjectResult MapResult<InputType, SucceededType, FailedType>(InputType result)
        where InputType : BaseResult
        where SucceededType : class
        where FailedType : class
    {
        return result.Success ?
            Ok(_mapper.From(result).AdaptToType<SucceededType>()) :
            BadRequest(_mapper.From(result).AdaptToType<FailedType>());
    }

    protected ObjectResult MapResult<InputType, SucceededType, FailedType>(InputType result, params (string, object)[] parameters)
        where InputType : BaseResult
        where SucceededType : class
        where FailedType : class
    {
        if (result.Success)
        {
            var builder = _mapper.From(result);

            if (parameters is not null)
                foreach (var parameter in parameters)
                {
                    builder = builder.AddParameters(parameter.Item1, parameter.Item2);
                }

            return Ok(builder.AdaptToType<SucceededType>());
        }
        else
        {
            return BadRequest(_mapper.From(result).AdaptToType<FailedType>());
        }
    }
}
