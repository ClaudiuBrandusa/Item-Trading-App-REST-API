# Item Trading App - REST API
The REST API of an app which will simulate the item trading system between some in-app items and a fake currency.

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
