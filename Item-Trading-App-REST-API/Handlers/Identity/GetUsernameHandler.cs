using Item_Trading_App_REST_API.Requests.Base;
using Item_Trading_App_REST_API.Services.Identity;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Item_Trading_App_REST_API.Requests.Identity;

public class GetUsernameHandler : HandlerBase, IRequestHandler<GetUsernameQuery, string>
{
    public GetUsernameHandler(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public async Task<string> Handle(GetUsernameQuery request, CancellationToken cancellationToken)
    {
        return await Execute<IIdentityService, string>(async (identityService) =>
            await identityService.GetUsername(request.UserId)
        );
    }
}
