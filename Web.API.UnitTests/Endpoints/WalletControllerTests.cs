using Application.Behaviors.Wallet.GetWallet;
using Application.Behaviors.Wallet.UpdateWallet;
using Application.Models.Common;
using Application.Results.Wallet;
using CommonTestUtils.Assertions;
using CommonTestUtils.Extensions;
using Domain.Entities.Identity;
using Item_Trading_App_Contracts.Requests.Wallet;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_Contracts.Responses.Wallet;
using Item_Trading_App_REST_API.Controllers;
using Item_Trading_App_REST_API.MappingConfigs;
using MapsterMapper;
using MediatR;
using Moq;

namespace Web.API.UnitTests.Endpoints;

public class WalletMappingFixture
{
    public Mapper Mapper { get; }

    public WalletMappingFixture()
    {
        Mapper = new Mapper();

        var itemMappingConfig = new WalletMappingConfig();
        itemMappingConfig.Register(Mapper.Config);
        var generalMappingConfig = new GeneralMappingConfig();
        generalMappingConfig.Register(Mapper.Config);
    }
}

public class WalletControllerTests : IClassFixture<WalletMappingFixture>
{
    private readonly Mapper _mapper;
    
    public WalletControllerTests(WalletMappingFixture fixture)
    {
        _mapper = fixture.Mapper;
    }

    [Fact]
    public async Task Get_GetUserWallet_RetrievesUserWallet()
    {
        // Arrange

        var expectedWalletResult = new WalletResult
        {
            UserId = "user-id",
            Cash = 10
        };

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<GetUserWalletQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetUserWalletQuery request, CancellationToken ct) =>
                Result<WalletResult>.Success(expectedWalletResult)
            );
        
        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new WalletController(_mapper, mediator);

        sut.SetSenderUser(user);

        // Act

        var result = await sut.Get();

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<WalletSuccessResponse>(result);
        Assert.Equal(expectedWalletResult.Cash, response.Cash);
    }

    [Fact]
    public async Task Get_AttemptGettingUserWallet_ShouldFail()
    {
        // Arrange

        var expectedWalletResult = new WalletResult
        {
            UserId = "user-id",
            Cash = 10
        };

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<GetUserWalletQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetUserWalletQuery request, CancellationToken ct) =>
                Result<WalletResult>.Failure("Something went wrong")
            );
        
        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new WalletController(_mapper, mediator);

        sut.SetSenderUser(user);

        // Act

        var result = await sut.Get();

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<FailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }

    [Fact]
    public async Task Update_UpdateUserWallet_UpdatesSuccessfully()
    {
        // Arrange

        var expectedWalletResult = new WalletResult
        {
            UserId = "user-id",
            Cash = 10
        };

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<UpdateWalletCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UpdateWalletCommand request, CancellationToken ct) =>
                Result<WalletResult>.Success(expectedWalletResult)
            );
        
        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new WalletController(_mapper, mediator);

        sut.SetSenderUser(user);

        var request = new UpdateWalletRequest
        {
            Quantity = 10
        };

        // Act

        var result = await sut.Update(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsResponse<UpdateWalletSuccessResponse>(result);
        Assert.Equal(expectedWalletResult.Cash, response.Amount);
    }

    [Fact]
    public async Task Update_AttemptUpdatingUserWallet_ShouldFail()
    {
        // Arrange

        var mediatorMock = new Mock<IMediator>();

        mediatorMock.Setup(x => x.Send(It.IsAny<UpdateWalletCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UpdateWalletCommand request, CancellationToken ct) =>
                Result<WalletResult>.Failure("Something went wrong")
            );
        
        var mediator = mediatorMock.Object;

        var user = new User
        {
            Id = User.GenerateId(),
            UserName = string.Empty
        };

        var sut = new WalletController(_mapper, mediator);

        sut.SetSenderUser(user);

        var request = new UpdateWalletRequest
        {
            Quantity = 10
        };

        // Act

        var result = await sut.Update(request);

        // Assert

        var response = HttpResultAssert.AssertActionResultAsFailedResponse<FailedResponse>(result);
        Assert.NotEmpty(response.Errors);
    }
}
