using System.Security.Claims;
using Domain.Entities.Identity;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_REST_API.Controllers;
using Microsoft.AspNetCore.Mvc;
using Web.API.IntegrationTests.Common.Factories;

namespace Web.API.IntegrationTests.Controllers.Common;

public static class Utils
{
    public static ClaimsPrincipal CreateDefaultUser()
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim("id", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, "Admin")
        ], authenticationType: "Test"));
    }

    public static ClaimsPrincipal CreateClaimsFromUser(User user)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim("id", user.Id),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.Role, "Admin")
        ], authenticationType: "Test"));
    }

    public static ControllerPack<T> CreateControllerPackWithUser<T>(TestAppFactory factory, ClaimsPrincipal user) where T : BaseController
    {
        var controllerPack = new ControllerPack<T>(factory);

        controllerPack.SetUser(user);

        return controllerPack;
    }

    public static ControllerPack<T> CreateControllerPackWithDefaultUser<T>(TestAppFactory factory) where T : BaseController
    {
        var controllerPack = new ControllerPack<T>(factory);

        var user = CreateDefaultUser();

        controllerPack.SetUser(user);

        return controllerPack;
    }

    public static T? GetContent<T>(IActionResult result) where T : class
    {
        var objectResult = result as ObjectResult;
        return objectResult?.Value as T;
    }

    public static ApiResponse<T,R> GetContent<T,R>(IActionResult result)
        where T : class
        where R : FailedResponse
    {
        var objectResult = result as ObjectResult;
        return new ApiResponse<T,R>(objectResult!);
    }
    
    public static OkObjectResult? AssertActionResultAsOkObjectResult(IActionResult? actionResult)
    {
        Assert.NotNull(actionResult);
        Assert.IsAssignableFrom<OkObjectResult>(actionResult);
        return (OkObjectResult)actionResult;
    }
    
    public static BadRequestObjectResult? AssertActionResultAsBadRequestObjectResult(IActionResult? actionResult)
    {
        Assert.NotNull(actionResult);
        Assert.IsAssignableFrom<BadRequestObjectResult>(actionResult);
        return (BadRequestObjectResult)actionResult;
    }

    public static T? AssertOkObjectResultSuccessResponse<T>(OkObjectResult? okObjectResult) where T : class
    {
        Assert.NotNull(okObjectResult);
        Assert.Equal(200, okObjectResult.StatusCode);
        Assert.IsAssignableFrom<T>(okObjectResult.Value);
        return okObjectResult.Value as T;
    }

    public static T? AssertBadRequestObjectResultFailedResponse<T>(BadRequestObjectResult? badRequestObjectResult) where T : class
    {
        Assert.NotNull(badRequestObjectResult);
        Assert.Equal(400, badRequestObjectResult.StatusCode);
        Assert.IsAssignableFrom<T>(badRequestObjectResult.Value);
        return badRequestObjectResult.Value as T;
    }

    public static void AssertResponseHasOnlyOneError<T>(T? response) where T : FailedResponse
    {
        Assert.NotNull(response);
        Assert.NotNull(response.Errors);
        var errors = response.Errors.ToArray();
        Assert.Single(response.Errors);
        Assert.NotEmpty(errors[0]);
    }
}
