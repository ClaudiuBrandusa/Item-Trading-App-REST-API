using System.Security.Claims;
using Domain.Entities.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Web.API.IntegrationTests.Controllers;

public static class Utils
{
    public static ClaimsPrincipal CreateDefaultUser()
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim("id", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, "Admin")
        }, authenticationType: "Test"));
    }

    public static ClaimsPrincipal CreateClaimsFromUser(User user)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim("id", user.Id),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.Role, "Admin")
        }, authenticationType: "Test"));
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
}
