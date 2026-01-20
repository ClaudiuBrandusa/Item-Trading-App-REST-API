using Item_Trading_App_Contracts.Responses.Item;
using Item_Trading_App_REST_API.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Web.API.IntegrationTests.Common.Factories;

namespace Web.API.IntegrationTests.Controllers;

public class ItemControllerTests : IClassFixture<TestAppFactory>
{
    private readonly TestAppFactory _factory;

    public ItemControllerTests(TestAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateItem_CreatesANewItem_ShouldCreateTheItemSuccessfully()
    {
        using var scope = _factory.Services.CreateScope();

        var controller = ActivatorUtilities.CreateInstance<ItemController>(scope.ServiceProvider);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim("id", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, "Admin")
        }, authenticationType: "Test"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                RequestServices = scope.ServiceProvider,
                User = user
            }
        };

        var request = new Item_Trading_App_Contracts.Requests.Item.CreateItemRequest
        {
            ItemName = "Test",
            ItemDescription = "Test"
        };

        var result = await controller.Create(request);

        Assert.NotNull(result);
        Assert.IsAssignableFrom<OkObjectResult>(result);
        var objectResult = (OkObjectResult)result;
        Assert.NotNull(objectResult);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.IsAssignableFrom<CreateItemSuccessResponse>(objectResult.Value);
        var response = objectResult.Value as CreateItemSuccessResponse;
        Assert.NotNull(response);
        Assert.NotEmpty(response.ItemId);
        Assert.Equal(request.ItemName, response.ItemName);
        Assert.Equal(request.ItemDescription, response.ItemDescription);
    }
}
