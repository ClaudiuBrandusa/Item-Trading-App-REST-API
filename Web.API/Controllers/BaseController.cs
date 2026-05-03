using Application.Extensions;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Application.Models.Common;
using Item_Trading_App_Contracts.Responses.Base;
using System;

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
        if (result is null)
        {
            return BadRequest(new FailedResponse
            {
                Errors = ["Something went wrong"]
            });
        }

        if (result.IsSuccess)
        {
            var mapped = AdaptToType<InputType, SucceededType>(result.Content!);
            return Ok(mapped);
        }

        var response = new FailedResponse { Errors = [result.Error]};

        if (typeof(FailedType) == typeof(FailedResponse))
            return BadRequest(response);

        return BadRequest(_mapper.From(response).AdaptToType<FailedType>());
    }
    
    protected ObjectResult MapResult<InputType, SucceededType, FailedType>(Result<InputType> result, Func<InputType, SucceededType> conversionMethod)
        where SucceededType : class
        where FailedType : FailedResponse
    {
        if (result is null)
        {
            return BadRequest(new FailedResponse
            {
                Errors = ["Something went wrong"]
            });
        }

        if (result.IsSuccess)
        {
            var mapped = conversionMethod(result.Content);
            return Ok(mapped);
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
            return Ok(AdaptToType<InputType, SucceededType>(result.Content!, parameters));
        }
        else
        {
            return BadRequest(_mapper.From(result.Error).AdaptToType<FailedType>());
        }
    }
}
