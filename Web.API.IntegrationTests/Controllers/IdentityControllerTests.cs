using Item_Trading_App_Contracts.Requests.Identity;
using Item_Trading_App_Contracts.Responses.Identity;
using Item_Trading_App_REST_API.Controllers;
using Microsoft.AspNetCore.Mvc;
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
        var controllerPack = CreateController(_factory);
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
        var objectResult = AssertActionResultAsOkObjectResult(registerResult);
        var authenticationResponse = AssertOkObjectResultSuccessResponse<AuthenticationSuccessResponse>(objectResult);

        Assert.NotNull(authenticationResponse);
        Assert.NotEmpty(authenticationResponse.Token);
        Assert.NotEmpty(authenticationResponse.RefreshToken);
        Assert.NotEqual(DateTime.MinValue, authenticationResponse.ExpirationDateTime);
    }

    [Fact]
    public async Task Login_RegisterNewUserThenLogin_ShouldExecuteSuccessfully()
    {
        var controllerPack = CreateController(_factory);
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
        var objectResult = AssertActionResultAsOkObjectResult(loginResult);
        var authenticationResponse = AssertOkObjectResultSuccessResponse<AuthenticationSuccessResponse>(objectResult);

        Assert.NotNull(authenticationResponse);
        Assert.NotEmpty(authenticationResponse.Token);
        Assert.NotEmpty(authenticationResponse.RefreshToken);
        Assert.NotEqual(DateTime.MinValue, authenticationResponse.ExpirationDateTime);
    }

    [Fact]
    public async Task Login_AttemptLoginWithInvalidData_ShouldFail()
    {
        var controllerPack = CreateController(_factory);
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
        var controllerPack = CreateController(_factory);
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
        var registerResponse = GetContent<AuthenticationSuccessResponse>(registerResult);
        
        var userId = _factory.GetUserIdFromToken(registerResponse!.Token);

        var getUsernameResult = await controller.GetUsername(userId);
        var objectResult = AssertActionResultAsOkObjectResult(getUsernameResult);
        var getUsernameResponse = AssertOkObjectResultSuccessResponse<UsernameSuccessResponse>(objectResult);

        Assert.NotNull(getUsernameResponse);
        Assert.Equal(userId, getUsernameResponse.UserId);
        Assert.Equal(expectedUsername, getUsernameResponse.Username);
    }

    public static T? GetContent<T>(IActionResult result) where T : class
    {
        var objectResult = result as ObjectResult;
        return objectResult?.Value as T;
    }

    private ControllerPack<IdentityController> CreateController(TestAppFactory factory)
    {
        return new ControllerPack<IdentityController>(factory);
    }

    private string GetUsername(int index) => $"New_User_{index}";

    private string GetEmail(int index) => $"newUser{index}@g.com";
}