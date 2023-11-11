using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Item_Trading_App_REST_API.Options;
using Item_Trading_App_REST_API.HostedServices.Identity.RefreshToken;
using Microsoft.Extensions.DependencyInjection;
using Item_Trading_App_REST_API.Hubs;
using Item_Trading_App_REST_API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.InstallServicesInAssembly(builder.Configuration);

builder.Services.AddHostedService<RefreshTokenHostedService>(); // had to run it here in order to have it executed after the rest of app has started

var app = builder.Build();

app.UseCors(options =>
    options.WithOrigins(builder.Configuration["AngularClientSettings:Client_URL"].ToString())
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials()
);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}

app.UseAuthentication();
var swaggerOptions = new SwaggerOptions();

builder.Configuration.GetSection(nameof(SwaggerOptions)).Bind(swaggerOptions);

app.UseSwagger(option =>
{
    option.RouteTemplate = swaggerOptions.JsonRoute;
});

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint(swaggerOptions.UIEndPoint, swaggerOptions.Description);
});

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseWebSockets();

app.MapControllers();

app.MapHub<NotificationHub>("/hubs/notification");

app.Run();