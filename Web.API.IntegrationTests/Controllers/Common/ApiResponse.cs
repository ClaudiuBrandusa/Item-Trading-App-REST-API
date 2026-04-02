using Microsoft.AspNetCore.Mvc;

namespace Web.API.IntegrationTests.Controllers.Common;

public class ApiResponse<SuccessfullResponse, FailedResponse>
    where SuccessfullResponse : class
    where FailedResponse : Item_Trading_App_Contracts.Responses.Base.FailedResponse
{
    public SuccessfullResponse? SuccessfullResponseContent { get; init; }

    public FailedResponse? FailedResponseContent { get; init; }

    public ApiResponse(ObjectResult? result)
    {
        if (result is null)
        {
            throw new ArgumentNullException("Object result is null");
        }

        if (result.Value is SuccessfullResponse sr)
        {
            SuccessfullResponseContent = sr;
        }
        else if (result.Value is FailedResponse fr)
        {
            FailedResponseContent = fr;
        }
        else
        {
            throw new Exception("Something went wrong. Content matches no response type.");
        }
    }
}