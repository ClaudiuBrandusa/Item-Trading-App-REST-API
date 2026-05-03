using CommonTestUtils.Extensions;
using CommonTestUtils.Wrappers;
using Item_Trading_App_Contracts.Requests.Identity;
using Item_Trading_App_Contracts.Responses.Identity;
using Item_Trading_App_REST_API.Controllers;
using Web.API.IntegrationTests.Common.Factories;
using static CommonTestUtils.Assertions.HttpResultAssert;
using static CommonTestUtils.Assertions.ResultPatternAssert;
using static CommonTestUtils.Utils.ControllerPackUtils;

namespace Web.API.IntegrationTests.Controllers;

public class IdentityControllerTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;
    private const string DefaultPassword = "!Abcd1234";

    public IdentityControllerTests(TestAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_RegisterNewUser_ShouldExecuteSuccessfully()
    {
        using var controllerPack = CreateController(_factory);
        var controller = controllerPack.ControllerInstance;

        var expectedUsername = GetUsername(0);
        var expectedEmail = GetEmail(0);

        var request = new UserRegisterRequest
        {
            Username = expectedUsername,
            Email = expectedEmail,
            Password = DefaultPassword,
            ConfirmPassword = DefaultPassword
        };

        var registerResult = await controller.Register(request);
        var receivedResponseDateTime = DateTime.UtcNow;

        var authenticationResponse = AssertActionResultAsResponse<AuthenticationSuccessResponse>(registerResult);
        Assert.NotNull(authenticationResponse);
        Assert.NotEmpty(authenticationResponse.Token);
        Assert.NotEmpty(authenticationResponse.RefreshToken);
        Assert.True(receivedResponseDateTime < authenticationResponse.ExpirationDateTime);
    }

    [Fact]
    public async Task Login_RegisterNewUserThenLogin_ShouldExecuteSuccessfully()
    {
        using var controllerPack = CreateController(_factory);
        var controller = controllerPack.ControllerInstance;

        var expectedUsername = GetUsername(1);
        var expectedEmail = GetEmail(1);

        var registerRequest = new UserRegisterRequest
        {
            Username = expectedUsername,
            Email = expectedEmail,
            Password = DefaultPassword,
            ConfirmPassword = DefaultPassword
        };

        await controller.Register(registerRequest);

        var loginRequest = new UserLoginRequest
        {
            Username = expectedUsername,
            Password = DefaultPassword  
        };

        var loginResult = await controller.Login(loginRequest);
        var receivedResponseDateTime = DateTime.UtcNow;

        var authenticationResponse = AssertActionResultAsResponse<AuthenticationSuccessResponse>(loginResult);
        Assert.NotNull(authenticationResponse);
        Assert.NotEmpty(authenticationResponse.Token);
        Assert.NotEmpty(authenticationResponse.RefreshToken);
        Assert.True(receivedResponseDateTime < authenticationResponse.ExpirationDateTime);
    }

    [Fact]
    public async Task Login_AttemptLoginWithInvalidData_ShouldFail()
    {
        using var controllerPack = CreateController(_factory);
        var controller = controllerPack.ControllerInstance;

        var loginRequest = new UserLoginRequest
        {
            Username = "Invalid_User",
            Password = DefaultPassword  
        };

        var loginResult = await controller.Login(loginRequest);

        var authenticationResponse = AssertActionResultAsFailedResponse<AuthenticationFailedResponse>(loginResult);
        AssertHasOnlyOneError(authenticationResponse);
    }

    [Fact]
    public async Task GetUsername_RegisterNewUserThenLogin_ShouldExecuteSuccessfully()
    {
        using var controllerPack = CreateController(_factory);
        var controller = controllerPack.ControllerInstance;

        var expectedUsername = GetUsername(2);
        var expectedEmail = GetEmail(2);

        var registerRequest = new UserRegisterRequest
        {
            Username = expectedUsername,
            Email = expectedEmail,
            Password = DefaultPassword,
            ConfirmPassword = DefaultPassword
        };

        var registerResult = await controller.Register(registerRequest);
        var receivedResponseDateTime = DateTime.UtcNow;
        var registerResponse = AssertActionResultAsResponse<AuthenticationSuccessResponse>(registerResult);
        
        var userId = _factory.GetUserIdFromToken(registerResponse!.Token);

        var getUsernameResult = await controller.GetUsername(userId);

        var getUsernameResponse = AssertActionResultAsResponse<UsernameSuccessResponse>(getUsernameResult);
        Assert.NotNull(getUsernameResponse);
        Assert.Equal(userId, getUsernameResponse.UserId);
        Assert.Equal(expectedUsername, getUsernameResponse.Username);
    }

    [Fact]
    public async Task Refresh_RegisterNewUserThenRefershToken_ShouldExecuteSuccessfully()
    {
        using var controllerPack = CreateController(_factory);
        var controller = controllerPack.ControllerInstance;

        var expectedUsername = GetUsername(3);
        var expectedEmail = GetEmail(3);

        var registerRequest = new UserRegisterRequest
        {
            Username = expectedUsername,
            Email = expectedEmail,
            Password = DefaultPassword,
            ConfirmPassword = DefaultPassword
        };

        var registerResult = await controller.Register(registerRequest);
        var registerResponse = AssertActionResultAsResponse<AuthenticationSuccessResponse>(registerResult);
        
        var refreshTokenRequest = new RefreshTokenRequest
        {
            Token = registerResponse!.Token,
            RefreshToken = registerResponse.RefreshToken
        };

        var refreshResult = await controller.Refresh(refreshTokenRequest);
        var receivedResponseDateTime = DateTime.UtcNow;

        var refreshTokenResponse = AssertActionResultAsResponse<AuthenticationSuccessResponse>(refreshResult);
        Assert.NotNull(refreshTokenResponse);
        Assert.NotEmpty(refreshTokenResponse.Token);
        Assert.NotEmpty(refreshTokenResponse.RefreshToken);
        Assert.True(receivedResponseDateTime < refreshTokenResponse.ExpirationDateTime);
        Assert.NotEqual(refreshTokenRequest.Token, refreshTokenResponse.Token);
        Assert.NotEqual(refreshTokenRequest.RefreshToken, refreshTokenResponse.RefreshToken);
    }

    [Fact]
    public async Task ListUsers_RegisterNewUserThenListUsers_ShouldExecuteSuccessfully()
    {
        using var dbContext = _factory.GetDatabaseContext();
        (var user, var userClaims) = dbContext.GetUserWithClaimsByName("Claudiu");

        using var controllerPack = CreateControllerPackWithUser<IdentityController, Program>(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var expectedUsername = GetUsername(4);
        var expectedEmail = GetEmail(4);

        var registerRequest = new UserRegisterRequest
        {
            Username = expectedUsername,
            Email = expectedEmail,
            Password = DefaultPassword,
            ConfirmPassword = DefaultPassword
        };

        await controller.Register(registerRequest);
        
        var listUsersResult = await controller.ListUsers(string.Empty);

        var listUsersResponse = AssertActionResultAsResponse<UsersSuccessResponse>(listUsersResult);
        Assert.NotNull(listUsersResponse);
        Assert.NotNull(listUsersResponse.UsersId);
        var userIds = listUsersResponse.UsersId.ToArray();
        Assert.True(userIds.Length > 0);
    }

    private ControllerPack<IdentityController, Program> CreateController(TestAppFactory factory)
    {
        return new ControllerPack<IdentityController, Program>(factory);
    }

    private string GetUsername(int index) => $"New_User_{index}";

    private string GetEmail(int index) => $"newUser{index}@g.com";
}