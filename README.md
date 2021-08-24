# Item Trading App - REST API
The REST API of an app which will simulate the item trading system between some in-app items and a fake currency.

## Structure
### Database
We are using Identity (from EntityFrameworkCore) so then we will have some tables used by it. We are not going to discuss about those tables because we can find details about them online (https://docs.microsoft.com/en-us/aspnet/identity/overview/getting-started/introduction-to-aspnet-identity).
<a href="https://drive.google.com/uc?export=view&id=1exLmGbJ7Cby21H9fOSRvp0wGe3JPsOwy"><img src="https://drive.google.com/uc?export=view&id=1exLmGbJ7Cby21H9fOSRvp0wGe3JPsOwy" style="width: 650px; max-width: 100%; height: auto" title="Click to enlarge picture" /></a>  
#### Users
This is the same table created by the Identity(Identity named it as AspNetUsers but I chose to keep it short in diagram so that I named it Users). 
- UserId is the user id used by Identity.
- Cash keeps the amount of the in-app currency that each user has.

#### Items
Contains details about each item available in the app.
- ItemId is the id of the item.
- Name holds the name of the item.
- Description holds the details about the item.

#### OwnedItems
Is the link table between Users and their items (from Items table).
- UserId is the foreign composite primary key from Users table.
- ItemId is the foreign composite primary key from Items table.
- Quantity is the amount of the item that the given user has.

#### Trades
Keeps data about each trade.
- TradeId is the ID of the trade.
- SentDate is the date when the trade was initiated.
- ResponseDate is the date when the trade request got a response. Until then, this field will remain null.
- Response is a binary field which represents the received response from the receiver.

#### SentTrades
Is the link table between Users and their initiated trades.
- TradeId is the primary foreign key from Trades table.
- SenderId is the ID of the user who initiated the trade.

#### ReceivedTrades
Is the link table between Users and their received trades.
- TradeId is the primary foreign key from Trades table.
- ReceiverId is the ID of the user who received the trade. Keep in mind that we can not receive a trade from ourselves (we cannot have the same row in SentTrades and in ReceivedTrades at the same time).

#### TradeContent
Is the link table between Trades and its item's details.
- TradeId is the foreign composite primary key from Trades table.
- ItemId is the foreign composite primary key from Items table.
- Quantity is the amount of the traded item.
- Price is the amount of the currency requested.

#### LockedItems
Used in order to lock the amount of items that is about to be traded.
- UserId is the id of the user who has the locked item.
- ItemId is the id of the item that is locked.
- Quantity is the amount of the locked items.

## Setup
#### Database
In order to setup the database you have to update it to the last migration.
You could do this by using PMC (Package Manager Console).  
`Update-Database`  
Or you could try the following command in Powershell.  
`dotnet ef database update`

## Tools used
- Visual Studio 2019
- Microsoft SQL Server Management Studio 18

## Technologies used
- ASP.NET Core 5

## Packages used
- Microsoft.EntityFrameworkCore (5.0.9)
- Microsoft.EntityFrameworkCore.SqlServer (5.0.9)
- Microsoft.EntityFrameworkCore.Tools (5.0.9)
- Microsoft.AspNetCore.Identity.EntityFrameworkCore (5.0.9)
- Microsoft.AspNetCore.Authentication.JwtBearer (5.0.9)
- Swashbuckle.AspNetCore (6.1.5)
- Swashbuckle.AspNetCore.Swagger (6.1.5)
- Swashbuckle.AspNetCore.SwaggerUI (6.1.5)
- Swashbuckle.Core (5.6.0)
