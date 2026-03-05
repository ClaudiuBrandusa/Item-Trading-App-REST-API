using Item_Trading_App_Contracts.Requests.Identity;
using Item_Trading_App_Contracts.Responses.Identity;
using Item_Trading_App_REST_API.Controllers;
using Web.API.IntegrationTests.Common.Factories;
using Web.API.IntegrationTests.Controllers.Common;
using static Web.API.IntegrationTests.Controllers.Common.Utils;

namespace Web.API.IntegrationTests.Controllers;

public class IdentityControllerTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;
    private const string _defaultPassword = "!Abcd1234";

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
            Password = _defaultPassword,
            ConfirmPassword = _defaultPassword
        };

        var registerResult = await controller.Register(request);
        var receivedResponseDateTime = DateTime.UtcNow;

        var objectResult = AssertActionResultAsOkObjectResult(registerResult);
        var authenticationResponse = AssertOkObjectResultSuccessResponse<AuthenticationSuccessResponse>(objectResult);
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
            Password = _defaultPassword,
            ConfirmPassword = _defaultPassword
        };

        await controller.Register(registerRequest);

        var loginRequest = new UserLoginRequest
        {
            Username = expectedUsername,
            Password = _defaultPassword  
        };

        var loginResult = await controller.Login(loginRequest);
        var receivedResponseDateTime = DateTime.UtcNow;

        var objectResult = AssertActionResultAsOkObjectResult(loginResult);
        var authenticationResponse = AssertOkObjectResultSuccessResponse<AuthenticationSuccessResponse>(objectResult);
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
            Password = _defaultPassword  
        };

        var loginResult = await controller.Login(loginRequest);

        var objectResult = AssertActionResultAsBadRequestObjectResult(loginResult);
        var authenticationResponse = AssertBadRequestObjectResultFailedResponse<AuthenticationFailedResponse>(objectResult);
        AssertResponseHasOnlyOneError(authenticationResponse);
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
            Password = _defaultPassword,
            ConfirmPassword = _defaultPassword
        };

        var registerResult = await controller.Register(registerRequest);
        var receivedResponseDateTime = DateTime.UtcNow;
        var registerResponse = GetContent<AuthenticationSuccessResponse>(registerResult);
        
        var userId = _factory.GetUserIdFromToken(registerResponse!.Token);

        var getUsernameResult = await controller.GetUsername(userId);

        var objectResult = AssertActionResultAsOkObjectResult(getUsernameResult);
        var getUsernameResponse = AssertOkObjectResultSuccessResponse<UsernameSuccessResponse>(objectResult);
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
            Password = _defaultPassword,
            ConfirmPassword = _defaultPassword
        };

        var registerResult = await controller.Register(registerRequest);
        var registerResponse = GetContent<AuthenticationSuccessResponse>(registerResult);
        
        var refreshTokenRequest = new RefreshTokenRequest
        {
            Token = registerResponse!.Token,
            RefreshToken = registerResponse.RefreshToken
        };

        var refreshResult = await controller.Refresh(refreshTokenRequest);
        var receivedResponseDateTime = DateTime.UtcNow;

        var objectResult = AssertActionResultAsOkObjectResult(refreshResult);
        var refreshTokenResponse = AssertOkObjectResultSuccessResponse<AuthenticationSuccessResponse>(objectResult);
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

        using var controllerPack = CreateControllerPackWithUser<IdentityController>(_factory, userClaims);
        var controller = controllerPack.ControllerInstance;

        var expectedUsername = GetUsername(4);
        var expectedEmail = GetEmail(4);

        var registerRequest = new UserRegisterRequest
        {
            Username = expectedUsername,
            Email = expectedEmail,
            Password = _defaultPassword,
            ConfirmPassword = _defaultPassword
        };

        await controller.Register(registerRequest);
        
        var listUsersResult = await controller.ListUsers(string.Empty);

        var objectResult = AssertActionResultAsOkObjectResult(listUsersResult);
        var listUsersResponse = AssertOkObjectResultSuccessResponse<UsersSuccessResponse>(objectResult);
        Assert.NotNull(listUsersResponse);
        Assert.NotNull(listUsersResponse.UsersId);
        var userIds = listUsersResponse.UsersId.ToArray();
        Assert.True(userIds.Length > 0);
    }

    private ControllerPack<IdentityController> CreateController(TestAppFactory factory)
    {
        return new ControllerPack<IdentityController>(factory);
    }

    private string GetUsername(int index) => $"New_User_{index}";

    private string GetEmail(int index) => $"newUser{index}@g.com";
}