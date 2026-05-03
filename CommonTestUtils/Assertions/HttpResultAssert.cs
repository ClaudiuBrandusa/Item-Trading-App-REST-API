using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace CommonTestUtils.Assertions;

public static class HttpResultAssert
{
    #region OkObjectResult

    public static OkObjectResult AssertActionResponseOkObjectResult(IActionResult actionResult)
    {
        return Assert.IsAssignableFrom<OkObjectResult>(actionResult);
    }

    public static T AssertOkObjectResultAsResponse<T>(OkObjectResult objectResult)
    {
        return Assert.IsAssignableFrom<T>(objectResult.Value);
    }

    public static T AssertActionResultAsResponse<T>(IActionResult actionResult)
    {
        var objectResult = AssertActionResponseOkObjectResult(actionResult);
        return AssertOkObjectResultAsResponse<T>(objectResult);
    }

    #endregion OkObjectResult

    #region BadRequestObjectResult
    
    public static BadRequestObjectResult AssertActionResponseBadRequestObjectResult(IActionResult actionResult)
    {
        return Assert.IsAssignableFrom<BadRequestObjectResult>(actionResult);
    }

    public static T AssertBadRequestObjectResultAsResponse<T>(BadRequestObjectResult objectResult)
    {
        return Assert.IsAssignableFrom<T>(objectResult.Value);
    }

    public static T AssertActionResultAsFailedResponse<T>(IActionResult actionResult)
    {
        var objectResult = AssertActionResponseBadRequestObjectResult(actionResult);
        return AssertBadRequestObjectResultAsResponse<T>(objectResult);
    }

    #endregion BadRequestObjectResult
}
