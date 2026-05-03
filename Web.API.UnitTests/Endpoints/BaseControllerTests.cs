using Application.Models.Common;
using CommonTestUtils.Assertions;
using CommonTestUtils.Extensions;
using Domain.Entities.Identity;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_REST_API.Controllers;
using Item_Trading_App_REST_API.MappingConfigs;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Web.API.UnitTests.Endpoints;

public class BaseControllerTests
{
    private readonly IMapper _mapper;

    public BaseControllerTests()
    {
        _mapper = new Mock<IMapper>().Object;
    }

    [Fact]
    public void Constructor_CheckMapperInstance_ShouldRetrieveTheSameInstanceOfMapper()
    {
        // Arrange

        var controller = new BaseControllerTestImpl(_mapper);
        
        // Act

        var retrievedMapper = controller.GetMapperInstance();

        // Assert

        Assert.Equal(_mapper, retrievedMapper);
    }

    [Fact]
    public void UserId_CheckIfControllerReturnsCorrectUserId_ShouldReturnUserId()
    {
        // Arrange

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var controller = new BaseControllerTestImpl(_mapper);

        controller.SetSenderUser(user);

        // Act

        var retrievedUserId = controller.GetUserId();

        // Assert

        Assert.Equal(user.Id, retrievedUserId);
    }

    [Fact]
    public void UserId_AttemptToGetUserIdWhenUnauthorized_ShouldFail()
    {
        // Arrange

        var controller = new BaseControllerTestImpl(_mapper);

        // Act

        var action = () => controller.GetUserId();

        // Assert

        Assert.Throws<NullReferenceException>(action);
    }

    public void AdaptToType_AdaptFromInputTypeToSpecifiedType_ShouldAdaptToSpecifiedType()
    {
        // Arrange

        var input = 10;
        var expectedOutput = (float)input;

        var controller = new BaseControllerTestImpl(_mapper);

        // Act

        var result = controller.AdaptToType<int, float>(input);

        // Assert

        Assert.Equal(expectedOutput, result);
    }

    [Fact]
    public void AdaptToType_AdaptFromInputTypeToSpecifiedTypeUsingParameters_ShouldAdaptToSpecifiedType()
    {
        // Arrange

        var config = new TypeAdapterConfig();

        config.ForType<ExampleInputClass, ExampleOutputClass>()
            .Map(dest => dest.Parameter, src => src.Parameter)
            .Map(dest => dest.SecondParameter, src => MapContext.Current!.Parameters[nameof(ExampleOutputClass.SecondParameter)]);

        var mapper = new Mapper(config);

        var input = new ExampleInputClass
        {
            Parameter = 10
        };

        var expectedOutput = new ExampleOutputClass
        {
            Parameter = input.Parameter,
            SecondParameter = "value"
        };

        var controller = new BaseControllerTestImpl(mapper);

        // Act

        var result = controller.AdaptToType<ExampleInputClass, ExampleOutputClass>(input, (nameof(ExampleOutputClass.SecondParameter), expectedOutput.SecondParameter));

        // Assert

        Assert.Equal(input.Parameter, result.Parameter);
        Assert.Equal(expectedOutput.SecondParameter, result.SecondParameter);
    }

    [Fact]
    public void AdaptToType_AttemptToAdaptFromInputTypeToSpecifiedTypeWithoutRequiredParameters_ShouldFail()
    {
        // Arrange

        var config = new TypeAdapterConfig();

        config.ForType<ExampleInputClass, ExampleOutputClass>()
            .Map(dest => dest.Parameter, src => src.Parameter)
            .Map(dest => dest.SecondParameter, src => MapContext.Current!.Parameters[nameof(ExampleOutputClass.SecondParameter)]);

        var mapper = new Mapper(config);

        var input = new ExampleInputClass
        {
            Parameter = 10
        };

        var expectedOutput = new ExampleOutputClass
        {
            Parameter = input.Parameter,
            SecondParameter = "value"
        };

        var controller = new BaseControllerTestImpl(mapper);

        // Act

        var action = () => controller.AdaptToType<ExampleInputClass, ExampleOutputClass>(input);

        // Assert

        Assert.Throws<NullReferenceException>(action);
    }

    [Fact]
    public void MapResult_MapInputTypeToSuceededTypeWithoutRequiredParameters_ShouldFail()
    {
        // Arrange

        var config = new TypeAdapterConfig();

        config.ForType<ExampleInputClass, ExampleOutputClass>()
            .Map(dest => dest.Parameter, src => src.Parameter)
            .Map(dest => dest.SecondParameter, src => MapContext.Current!.Parameters[nameof(ExampleOutputClass.SecondParameter)]);

        var mapper = new Mapper(config);

        var input = new ExampleInputClass
        {
            Parameter = 10
        };

        var expectedOutput = new ExampleOutputClass
        {
            Parameter = input.Parameter,
            SecondParameter = "value"
        };

        var controller = new BaseControllerTestImpl(mapper);

        var inputResult = Result<ExampleInputClass>.Success(input);

        // Act

        var action = () => controller.MapResult<ExampleInputClass, ExampleOutputClass, FailedResponse>(inputResult);

        // Assert

        Assert.Throws<NullReferenceException>(action);
    }

    [Fact]
    public void MapResult_MapInputTypeToFailedTypeWithoutRequiredParameters_ShouldSucceed()
    {
        // Arrange

        var config = new TypeAdapterConfig();

        config.ForType<ExampleInputClass, ExampleOutputClass>()
            .Map(dest => dest.Parameter, src => src.Parameter)
            .Map(dest => dest.SecondParameter, src => MapContext.Current!.Parameters[nameof(ExampleOutputClass.SecondParameter)]);

        var mapper = new Mapper(config);

        var input = new ExampleInputClass
        {
            Parameter = 10
        };

        var expectedOutput = new ExampleOutputClass
        {
            Parameter = input.Parameter,
            SecondParameter = "value"
        };

        var controller = new BaseControllerTestImpl(mapper);

        var inputResult = Result<ExampleInputClass>.Failure("Something went wrong");

        // Act

        var result = controller.MapResult<ExampleInputClass, ExampleOutputClass, FailedResponse>(inputResult);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<FailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }

    [Fact]
    public void MapResult_MapInputTypeToSuceededTypeWithConversionMethod_ShouldMapSuccessfully()
    {
        // Arrange

        var mapper = new Mapper();

        var input = new ExampleInputClass
        {
            Parameter = 10
        };

        var expectedOutput = new ExampleOutputClass
        {
            Parameter = input.Parameter,
            SecondParameter = "value"
        };

        var controller = new BaseControllerTestImpl(mapper);

        var inputResult = Result<ExampleInputClass>.Success(input);

        // Act

        var result = controller.MapResult<ExampleInputClass, ExampleOutputClass, FailedResponse>(inputResult, (ExampleInputClass x) =>
            new ExampleOutputClass
            {
                Parameter = x.Parameter,
                SecondParameter = expectedOutput.SecondParameter
            });

        // Assert

        var response = HttpResultAssert.AssertOkObjectResultAsResponse<ExampleOutputClass>((result as OkObjectResult)!);
        Assert.Equal(expectedOutput.Parameter, response.Parameter);
        Assert.Equal(expectedOutput.SecondParameter, response.SecondParameter);
    }

    [Fact]
    public void MapResult_MapInputTypeToFailedTypeWithConversionMethod_ShouldMapSuccessfully()
    {
        // Arrange

        var mapper = new Mapper();

        var input = new ExampleInputClass
        {
            Parameter = 10
        };

        var expectedOutput = new ExampleOutputClass
        {
            Parameter = input.Parameter,
            SecondParameter = "value"
        };

        var controller = new BaseControllerTestImpl(mapper);

        var inputResult = Result<ExampleInputClass>.Failure("Something went wrong");

        // Act

        var result = controller.MapResult<ExampleInputClass, ExampleOutputClass, FailedResponse>(inputResult, (ExampleInputClass x) =>
            new ExampleOutputClass
            {
                Parameter = x.Parameter,
                SecondParameter = expectedOutput.SecondParameter
            });

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<FailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }

    [Fact]
    public void MapResult_MapInputTypeWithParametersToSuceededType_ShouldMapSuccessfully()
    {
        // Arrange

        var config = new TypeAdapterConfig();

        config.ForType<ExampleInputClass, ExampleOutputClass>()
            .Map(dest => dest.Parameter, src => src.Parameter)
            .Map(dest => dest.SecondParameter, src => MapContext.Current!.Parameters[nameof(ExampleOutputClass.SecondParameter)]);

        var mapper = new Mapper(config);

        var input = new ExampleInputClass
        {
            Parameter = 10
        };

        var expectedOutput = new ExampleOutputClass
        {
            Parameter = input.Parameter,
            SecondParameter = "value"
        };

        var controller = new BaseControllerTestImpl(mapper);

        var inputResult = Result<ExampleInputClass>.Success(input);

        // Act

        var result = controller.MapResult<ExampleInputClass, ExampleOutputClass, FailedResponse>(inputResult, (nameof(ExampleOutputClass.SecondParameter), expectedOutput.SecondParameter));

        // Assert

        var response = HttpResultAssert.AssertOkObjectResultAsResponse<ExampleOutputClass>((result as OkObjectResult)!);
        Assert.Equal(expectedOutput.Parameter, response.Parameter);
        Assert.Equal(expectedOutput.SecondParameter, response.SecondParameter);
    }

    [Fact]
    public void MapResult_MapInputTypeWithParametersToFailedType_ShouldMapSuccessfully()
    {
        // Arrange

        var config = new TypeAdapterConfig();

        config.ForType<ExampleInputClass, ExampleOutputClass>()
            .Map(dest => dest.Parameter, src => src.Parameter)
            .Map(dest => dest.SecondParameter, src => MapContext.Current!.Parameters[nameof(ExampleOutputClass.SecondParameter)]);

        new GeneralMappingConfig().Register(config);

        var mapper = new Mapper(config);

        var input = new ExampleInputClass
        {
            Parameter = 10
        };

        var expectedOutput = new ExampleOutputClass
        {
            Parameter = input.Parameter,
            SecondParameter = "value"
        };

        var controller = new BaseControllerTestImpl(mapper);

        var inputResult = Result<ExampleInputClass>.Failure("Something went wrong");

        // Act

        var result = controller.MapResult<ExampleInputClass, ExampleOutputClass, FailedResponse>(inputResult, (nameof(ExampleOutputClass.SecondParameter), expectedOutput.SecondParameter));

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<FailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }

    public class BaseControllerTestImpl : BaseController
    {
        public BaseControllerTestImpl(IMapper mapper) : base(mapper)
        {
        }

        public IMapper GetMapperInstance()
        {
            return _mapper;
        }

        public string GetUserId()
        {
            return UserId;
        }

        public R AdaptToType<T, R>(T request, params (string, object)[] parameters) =>
            base.AdaptToType<T,R>(request, parameters);

        public ObjectResult MapResult<InputType, SucceededType, FailedType>(Result<InputType> result)
            where SucceededType : class
            where FailedType : FailedResponse
            => base.MapResult<InputType, SucceededType, FailedType>(result);

        public ObjectResult MapResult<InputType, SucceededType, FailedType>(Result<InputType> result, Func<InputType, SucceededType> conversionMethod)
            where SucceededType : class
            where FailedType : FailedResponse
            => base.MapResult<InputType, SucceededType, FailedType>(result, conversionMethod);

        public ObjectResult MapResult<InputType, SucceededType, FailedType>(Result<InputType> result, params (string, object)[] parameters)
            where SucceededType : class
            where FailedType : class
            => base.MapResult<InputType, SucceededType, FailedType>(result, parameters);
    }

    public class ExampleInputClass
    {
        public int Parameter { get; set; }
    }

    public class ExampleOutputClass
    {
        public int Parameter { get; set; }

        public string SecondParameter { get; set; }
    }
}
