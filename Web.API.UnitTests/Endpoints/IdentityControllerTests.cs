using Application.Behaviors.Identity.GetUsername;
using Application.Behaviors.Identity.ListUsers;
using Application.Behaviors.Identity.LoginUser;
using Application.Behaviors.Identity.RefreshToken;
using Application.Behaviors.Identity.RegisterUser;
using Application.Models.Common;
using Application.Results.Identity;
using CommonTestUtils.Assertions;
using CommonTestUtils.Extensions;
using Domain.Entities.Identity;
using Item_Trading_App_Contracts.Requests.Identity;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_Contracts.Responses.Identity;
using Item_Trading_App_REST_API.Controllers;
using Item_Trading_App_REST_API.MappingConfigs;
using MapsterMapper;
using MediatR;
using Moq;

namespace Web.API.UnitTests.Endpoints;

public class IdentityMappingFixture
{
    public Mapper Mapper { get; }

    public IdentityMappingFixture()
    {
        Mapper = new Mapper();

        var itemMappingConfig = new IdentityMappingConfig();
        itemMappingConfig.Register(Mapper.Config);
        var generalMappingConfig = new GeneralMappingConfig();
        generalMappingConfig.Register(Mapper.Config);
    }
}

public class IdentityControllerTests : IClassFixture<IdentityMappingFixture>
{
    private readonly Mapper _mapper;
    
    public IdentityControllerTests(IdentityMappingFixture fixture)
    {
        _mapper = fixture.Mapper;
    }

    [Fact]
    public async Task Register_RegisterNewAccount_ShouldSucceed()
    {
        // Arrange
        
        var mediatorMock = new Mock<IMediator>();

        var expectedAuthenticationResult = new AuthenticationResult("token", "refreshToken", DateTime.UtcNow);

        mediatorMock.Setup(x => x.Send(It.IsAny<RegisterCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RegisterCommand command, CancellationToken ct) =>
            Result<AuthenticationResult>.Success(expectedAuthenticationResult));

        var mediator = mediatorMock.Object;

        var sut = new IdentityController(_mapper, mediator);

        var request = new UserRegisterRequest
        {
            Username = "username",
            Email = "a@a.com",
            Password = "a",
            ConfirmPassword = "a"
        };
        
        // Act

        var result = await sut.Register(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<AuthenticationSuccessResponse>(result);
        Assert.Equal(expectedAuthenticationResult.Token, response.Token);
        Assert.Equal(expectedAuthenticationResult.RefreshToken, response.RefreshToken);
        Assert.Equal(expectedAuthenticationResult.ExpirationDateTime, response.ExpirationDateTime);
    }

    [Fact]
    public async Task Register_AttemptRegisteringNewAccount_ShouldFail()
    {
        // Arrange
        
        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<RegisterCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RegisterCommand command, CancellationToken ct) =>
            Result<AuthenticationResult>.Failure("Something went wrong"));

        var mediator = mediatorMock.Object;

        var sut = new IdentityController(_mapper, mediator);

        var request = new UserRegisterRequest
        {
            Username = "username",
            Email = "a@a.com",
            Password = "a",
            ConfirmPassword = "a"
        };
        
        // Act

        var result = await sut.Register(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<AuthenticationFailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }

    [Fact]
    public async Task Login_LoginAccount_ShouldSucceed()
    {
        // Arrange
        
        var mediatorMock = new Mock<IMediator>();

        var expectedAuthenticationResult = new AuthenticationResult("token", "refreshToken", DateTime.UtcNow);

        mediatorMock.Setup(x => x.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoginCommand command, CancellationToken ct) =>
            Result<AuthenticationResult>.Success(expectedAuthenticationResult));

        var mediator = mediatorMock.Object;

        var sut = new IdentityController(_mapper, mediator);

        var request = new UserLoginRequest
        {
            Username = "username",
            Password = "a"
        };
        
        // Act

        var result = await sut.Login(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<AuthenticationSuccessResponse>(result);
        Assert.Equal(expectedAuthenticationResult.Token, response.Token);
        Assert.Equal(expectedAuthenticationResult.RefreshToken, response.RefreshToken);
        Assert.Equal(expectedAuthenticationResult.ExpirationDateTime, response.ExpirationDateTime);
    }

    [Fact]
    public async Task Login_AttemptLoginAccount_ShouldFail()
    {
        // Arrange
        
        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<LoginCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoginCommand command, CancellationToken ct) =>
            Result<AuthenticationResult>.Failure("Something went wrong"));

        var mediator = mediatorMock.Object;

        var sut = new IdentityController(_mapper, mediator);

        var request = new UserLoginRequest
        {
            Username = "username",
            Password = "a"
        };
        
        // Act

        var result = await sut.Login(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<AuthenticationFailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }

    [Fact]
    public async Task Refresh_RefreshAccount_ShouldSucceed()
    {
        // Arrange
        
        var mediatorMock = new Mock<IMediator>();

        var expectedAuthenticationResult = new AuthenticationResult("token", "refreshToken", DateTime.UtcNow);

        mediatorMock.Setup(x => x.Send(It.IsAny<RefreshTokenCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshTokenCommand command, CancellationToken ct) =>
            Result<AuthenticationResult>.Success(expectedAuthenticationResult));

        var mediator = mediatorMock.Object;

        var sut = new IdentityController(_mapper, mediator);

        var request = new RefreshTokenRequest
        {
            Token = "token",
            RefreshToken = "refreshToken"
        };
        
        // Act

        var result = await sut.Refresh(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<AuthenticationSuccessResponse>(result);
        Assert.Equal(expectedAuthenticationResult.Token, response.Token);
        Assert.Equal(expectedAuthenticationResult.RefreshToken, response.RefreshToken);
        Assert.Equal(expectedAuthenticationResult.ExpirationDateTime, response.ExpirationDateTime);
    }

    [Fact]
    public async Task Refresh_AttemptRefreshAccount_ShouldFail()
    {
        // Arrange
        
        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<RefreshTokenCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshTokenCommand command, CancellationToken ct) =>
            Result<AuthenticationResult>.Failure("Something went wrong"));

        var mediator = mediatorMock.Object;

        var sut = new IdentityController(_mapper, mediator);

        var request = new RefreshTokenRequest
        {
            Token = "token",
            RefreshToken = "refreshToken"
        };
        
        // Act

        var result = await sut.Refresh(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<AuthenticationFailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }

    [Fact]
    public async Task GetUsername_RetrieveUsernameOfAccount_ShouldSucceed()
    {
        // Arrange
        
        var mediatorMock = new Mock<IMediator>();

        var expectedUsername = "username";

        mediatorMock.Setup(x => x.Send(It.IsAny<GetUsernameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetUsernameQuery command, CancellationToken ct) =>
            Result<string>.Success(expectedUsername));

        var mediator = mediatorMock.Object;

        var sut = new IdentityController(_mapper, mediator);

        var userId = "user-id";
        
        // Act

        var result = await sut.GetUsername(userId);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<UsernameSuccessResponse>(result);
        Assert.Equal(userId, response.UserId);
        Assert.Equal(expectedUsername, response.Username);
    }

    [Fact]
    public async Task GetUsername_AttemptRetrievingTheUsernameOfAccountWithInvalidUserId_ShouldFail()
    {
        // Arrange
        
        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<GetUsernameQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetUsernameQuery command, CancellationToken ct) =>
            Result<string>.Failure("Something went wrong"));

        var mediator = mediatorMock.Object;

        var sut = new IdentityController(_mapper, mediator);

        var userId = "user-id";
        
        // Act

        var result = await sut.GetUsername(userId);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<FailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }

    [Fact]
    public async Task GetUsername_AttemptRetrievingTheUsernameOfAccount_ShouldFail()
    {
        // Arrange
        
        var mediator = new Mock<IMediator>().Object;

        var sut = new IdentityController(_mapper, mediator);

        var userId = string.Empty;
        
        // Act

        var result = await sut.GetUsername(userId);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<FailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }

    [Fact]
    public async Task ListUsers_RegisterNewAccount_ShouldSucceed()
    {
        // Arrange
        
        var mediatorMock = new Mock<IMediator>();

        var expectedUserIds = new string[] { "user-id-0", "user-id-1", "user-id-2" };

        mediatorMock.Setup(x => x.Send(It.IsAny<ListUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ListUsersQuery command, CancellationToken ct) =>
            Result<UsersResult>.Success(new UsersResult
            {
                UsersId = expectedUserIds
            }));

        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new IdentityController(_mapper, mediator);

        sut.SetSenderUser(user);

        var searchString = "user-id";
        
        // Act

        var result = await sut.ListUsers(searchString);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<UsersSuccessResponse>(result);
        Assert.All(expectedUserIds, expectedUserId => response.UsersId.Contains(expectedUserId));
    }

    [Fact]
    public async Task ListUsers_AttemptRegisteringNewAccount_ShouldFail()
    {
        // Arrange
        
        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<ListUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ListUsersQuery command, CancellationToken ct) =>
            Result<UsersResult>.Failure("Something went wrong"));

        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new IdentityController(_mapper, mediator);

        sut.SetSenderUser(user);

        var searchString = "user-id";
        
        // Act

        var result = await sut.ListUsers(searchString);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<FailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }
}
