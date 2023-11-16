using Item_Trading_App_Contracts.Base.Item;
using Item_Trading_App_Contracts.Requests.Inventory;
using Item_Trading_App_Contracts.Requests.Item;
using Item_Trading_App_Contracts.Requests.Trade;
using Item_Trading_App_Contracts.Requests.Wallet;
using Item_Trading_App_Contracts.Responses.Base;
using Item_Trading_App_Contracts.Responses.Identity;
using Item_Trading_App_Contracts.Responses.Inventory;
using Item_Trading_App_Contracts.Responses.Item;
using Item_Trading_App_Contracts.Responses.Trade;
using Item_Trading_App_REST_API.Models.Identity;
using Item_Trading_App_REST_API.Models.Inventory;
using Item_Trading_App_REST_API.Models.Item;
using Item_Trading_App_REST_API.Models.Trade;
using Item_Trading_App_REST_API.Models.Wallet;
using Item_Trading_App_REST_API.Requests.Wallet;
using Mapster;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;

namespace Item_Trading_App_REST_API.Installers;

public class MapsterInstaller : IInstaller
{
    public void InstallServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMapster();

        TypeAdapterConfig<ModelStateDictionary, FailedResponse>
            .NewConfig()
            .MapWith(dictionary => new FailedResponse { Errors = dictionary.Values.SelectMany(x => x.Errors.Select(xx => xx.ErrorMessage)) });

        // identity

        TypeAdapterConfig<ModelStateDictionary, AuthenticationFailedResponse>
            .NewConfig()
            .MapWith(dictionary => new AuthenticationFailedResponse { Errors = dictionary.Values.SelectMany(x => x.Errors.Select(xx => xx.ErrorMessage)) });

        TypeAdapterConfig<string, UsernameSuccessResponse>
            .NewConfig()
            .MapWith(str => new UsernameSuccessResponse { UserId = str, Username = MapContext.Current!.Parameters["username"].ToString() });

        TypeAdapterConfig<string, ListUsers>
            .NewConfig()
            .MapWith(str => new ListUsers { SearchString = str, UserId = MapContext.Current!.Parameters["userId"].ToString() });

        // item

        TypeAdapterConfig<FullItemResult, ItemResponse>
            .NewConfig()
            .Map(dest => dest.Id, src => src.ItemId)
            .Map(dest => dest.Name, src => src.ItemName)
            .Map(dest => dest.Description, src => src.ItemDescription);

        TypeAdapterConfig<CreateItemRequest, CreateItem>
            .NewConfig()
            .Map(dest => dest.SenderUserId, src => MapContext.Current!.Parameters["userId"]);

        TypeAdapterConfig<UpdateItemRequest, UpdateItem>
            .NewConfig()
            .Map(dest => dest.SenderUserId, src => MapContext.Current!.Parameters["userId"]);

        // inventory

        TypeAdapterConfig<AddItemRequest, AddItem>
            .NewConfig()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters["userId"]);

        TypeAdapterConfig<QuantifiedItemResult, AddItemFailedResponse>
            .NewConfig()
            .IgnoreNonMapped(true)
            .Map(dest => dest.Errors, src => src.Errors);

        TypeAdapterConfig<DropItemRequest, DropItem>
            .NewConfig()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters["userId"])
            .Map(dest => dest.Quantity, src => src.ItemQuantity);

        TypeAdapterConfig<string, GetUsersItem>
            .NewConfig()
            .MapWith(str =>
                new GetUsersItem { ItemId = str, UserId = MapContext.Current!.Parameters["userId"].ToString() });

        TypeAdapterConfig<string, ListItems>
            .NewConfig()
            .MapWith(str => new ListItems { SearchString = str, UserId = MapContext.Current!.Parameters["userId"].ToString() });

        // trade

        TypeAdapterConfig<string, RequestTradeOffer>
            .NewConfig()
            .MapWith(str =>
                new RequestTradeOffer { TradeOfferId = str, UserId = MapContext.Current!.Parameters["userId"].ToString() });

        TypeAdapterConfig<SentTradeOffer, GetSentTradeOfferSuccessResponse>
            .NewConfig()
            .Map(dest => dest.TradeId, src => src.TradeOfferId)
            .Map(dest => dest.Items, src => src.Items.Select(i => new ItemWithPrice { Id = i.ItemId, Name = i.Name, Price = i.Price, Quantity = i.Quantity }));

        TypeAdapterConfig<SentRespondedTradeOffer, GetSentRespondedTradeOfferSuccessResponse>
            .NewConfig()
            .Map(dest => dest.TradeId, src => src.TradeOfferId)
            .Map(dest => dest.Items, src => src.Items.Select(i => new ItemWithPrice { Id = i.ItemId, Name = i.Name, Price = i.Price, Quantity = i.Quantity }));

        TypeAdapterConfig<ReceivedTradeOffer, GetReceivedTradeOfferSuccessResponse>
            .NewConfig()
            .Map(dest => dest.TradeId, src => src.TradeOfferId)
            .Map(dest => dest.Items, src => src.Items.Select(i => new ItemWithPrice { Id = i.ItemId, Name = i.Name, Price = i.Price, Quantity = i.Quantity }));

        TypeAdapterConfig<ReceivedRespondedTradeOffer, GetReceivedRespondedTradeOfferSuccessResponse>
            .NewConfig()
            .Map(dest => dest.TradeId, src => src.TradeOfferId)
            .Map(dest => dest.Items, src => src.Items.Select(i => new ItemWithPrice { Id = i.ItemId, Name = i.Name, Price = i.Price, Quantity = i.Quantity }));

        TypeAdapterConfig<TradeOffersResult, ListTradeOffersSuccessResponse>
            .NewConfig()
            .Map(dest => dest.TradeOffersIds, src => src.TradeOffers);

        TypeAdapterConfig<TradeOfferRequest, CreateTradeOffer>
            .NewConfig()
            .Map(dest => dest.SenderUserId, src => MapContext.Current!.Parameters["userId"].ToString())
            .Map(dest => dest.Items, src => src.Items.Select(t => new ItemPrice { ItemId = t.Id, Price = t.Price, Quantity = t.Quantity }));

        TypeAdapterConfig<AcceptTradeOfferRequest, RespondTrade>
            .NewConfig()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters["userId"].ToString());

        TypeAdapterConfig<RejectTradeOfferRequest, RespondTrade>
            .NewConfig()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters["userId"].ToString());
        
        TypeAdapterConfig<RejectTradeOfferResult, RejectTradeOfferSuccessResponse>
            .NewConfig()
            .Map(dest => dest.Id, src => src.TradeOfferId);

        TypeAdapterConfig<CancelTradeOfferRequest, RespondTrade>
            .NewConfig()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters["userId"].ToString());

        // wallet

        TypeAdapterConfig<UpdateWalletRequest, UpdateWallet>
            .NewConfig()
            .Map(dest => dest.UserId, src => MapContext.Current!.Parameters["userId"].ToString());

        TypeAdapterConfig<GiveCashQuery, UpdateWallet>
            .NewConfig()
            .Map(dest => dest.Quantity, src => src.Amount);

        TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
    }
}
