using Item_Trading_App_REST_API.Middlewares;
using Item_Trading_App_REST_API.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;

namespace Item_Trading_App_REST_API.Filters;

public class StartupFilter : IStartupFilter
{
    private readonly CacheSettings cacheSettings = new();

    public StartupFilter(IConfiguration configuration)
    {
        configuration.Bind(nameof(CacheSettings), cacheSettings);
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            if (cacheSettings.InitAtStartup)
                builder.UseMiddleware<CacheInitMiddleware>();
            next(builder);
        };
    }
}
