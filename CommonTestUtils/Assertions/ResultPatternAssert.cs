using Item_Trading_App_Contracts.Responses.Base;
using Xunit;

namespace CommonTestUtils.Assertions;

public static class ResultPatternAssert
{
    public static void AssertHasOnlyOneError<T>(T? result) where T : FailedResponse
    {
        Assert.NotNull(result);
        Assert.NotNull(result.Errors);
        var errors = result.Errors.ToArray();
        Assert.Single(result.Errors);
        Assert.NotEmpty(errors[0]);
    }
}