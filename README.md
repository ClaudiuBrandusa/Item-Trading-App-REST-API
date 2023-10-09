# Item Trading App - REST API
The REST API of an app which will simulate the item trading system between some in-app items and a fake currency.

# Structure
## Database
We are using Identity (from EntityFrameworkCore) so then we will have some tables used by it. We are not going to discuss about those tables because we can find details about them online (https://docs.microsoft.com/en-us/aspnet/identity/overview/getting-started/introduction-to-aspnet-identity).
<a href="https://drive.google.com/uc?export=view&id=1exLmGbJ7Cby21H9fOSRvp0wGe3JPsOwy"><img src="https://drive.google.com/uc?export=view&id=1exLmGbJ7Cby21H9fOSRvp0wGe3JPsOwy" style="width: 650px; max-width: 100%; height: auto" title="Click to enlarge picture" /></a>  
### Users
This is the same table created by the Identity(Identity named it as AspNetUsers but I chose to keep it short in diagram so that I named it Users). 
- UserId is the user id used by Identity.
- Cash keeps the amount of the in-app currency that each user has.

### Items
Contains details about each item available in the app.
- ItemId is the id of the item.
- Name holds the name of the item.
- Description holds the details about the item.

### OwnedItems
Is the link table between Users and their items (from Items table).
- UserId is the foreign composite primary key from Users table.
- ItemId is the foreign composite primary key from Items table.
- Quantity is the amount of the item that the given user has.

### Trades
Keeps data about each trade.
- TradeId is the ID of the trade.
- SentDate is the date when the trade was initiated.
- ResponseDate is the date when the trade request got a response. Until then, this field will remain null.
- Response is a binary field which represents the received response from the receiver.

### SentTrades
Is the link table between Users and their initiated trades.
- TradeId is the primary foreign key from Trades table.
- SenderId is the ID of the user who initiated the trade.

### ReceivedTrades
Is the link table between Users and their received trades.
- TradeId is the primary foreign key from Trades table.
- ReceiverId is the ID of the user who received the trade. Keep in mind that we can not receive a trade from ourselves (we cannot have the same row in SentTrades and in ReceivedTrades at the same time).

### TradeContent
Is the link table between Trades and its item's details.
- TradeId is the foreign composite primary key from Trades table.
- ItemId is the foreign composite primary key from Items table.
- Quantity is the amount of the traded item.
- Price is the amount of the currency requested.

### LockedItems
Used in order to lock the amount of items that is about to be traded.
- UserId is the id of the user who has the locked item.
- ItemId is the id of the item that is locked.
- Quantity is the amount of the locked items.

## Actions
Action example: 
Action name (action description)
`endpoint`
### Identity  
[IdentityController.cs](Item-Trading-App-REST-API/Controllers/IdentityController.cs)
- Register (registers the user) `identity/register`
- Login (connects the user) `identity/login`
- Refresh (refreshes the user's token) `identity/refresh`
- GetUsername (returns the username with the given user id) `identity/get_username`
- ListUsers (returns a list with all of the registered users' id) `identity/list_users`

### Inventory  
[InventoryController.cs](Item-Trading-App-REST-API/Controllers/InventoryController.cs)
- Add (adds an item to the user's inventory) `inventory/add`
- Drop (removes a given amount of the item from the user's inventory) `inventory/drop`
- Get (returns details about an item from the user's inventory) `inventory/get`
- List (returns a list of all items id from the user's inventory) `inventory/list`

### Item  
[ItemController.cs](Item-Trading-App-REST-API/Controllers/ItemController.cs)
- Get (returns details about an item) `item/get`
- List (returns a list of all items id) `item/list`
- Create (creates a new item) `item/create`
- Update (updates data about a given item) `item/update`
- Delete (deletes an item) `item/delete`

### Trade  
[TradeController.cs](Item-Trading-App-REST-API/Controllers/TradeController.cs)
- GetSent (returns a sent trade offer) `trade/get_sent`
- GetSentResponded (returns a sent trade offer which was responded) `trade/get_sent_responded`
- GetReceived (returns a received trade offer) `trade/get_received`
- GetReceivedResponded (returns a received trade offer which was responded) `trade/get_received_responded`
- ListSent (returns a list of all sent trade offers) `trade/list_sent`
- ListSentResponded (returns a list of all sent and responded trade offers) `trade/list_sent_responded`
- ListReceived (returns a list of all received trade offers) `trade/list_received`
- ListReceivedResponded (returns a list of all received and responded trade offers) `trade/list_received_responded`
- Offer (creates a new trade offer) `trade/offer`
- Accept (accepts a received trade offer) `trade/accept`
- Reject (rejects a received trade offer) `trade/reject`
- Cancel (cancels a sent trade offer) `trade/cancel`

### Wallet  
[WalletController.cs](Item-Trading-App-REST-API/Controllers/WalletController.cs)
- Get (returns data about the user's wallet) `wallet/get`
- Update (updates the user's wallet data) `wallet/update`

# Setup
### Database
In order to setup the database you have to update it to the last migration.
You could do this by using PMC (Package Manager Console).  
`Update-Database`  
Or you could try the following command in Powershell.  
`dotnet ef database update`

# Tools used
- Visual Studio 2022
- Microsoft SQL Server Management Studio 18

# Technologies used
- ASP.NET Core 7

# Packages used
- MediatR (12.1.1)
- Microsoft.EntityFrameworkCore (7.0.11)
- Microsoft.EntityFrameworkCore.SqlServer (7.0.11)
- Microsoft.EntityFrameworkCore.Tools (7.0.11)
- Microsoft.AspNetCore.Identity.EntityFrameworkCore (7.0.11)
- Microsoft.AspNetCore.Authentication.JwtBearer (7.0.11)
- Swashbuckle.AspNetCore (6.5.0)
- Swashbuckle.AspNetCore.Swagger (6.5.0)
- Swashbuckle.AspNetCore.SwaggerUI (6.5.0)
