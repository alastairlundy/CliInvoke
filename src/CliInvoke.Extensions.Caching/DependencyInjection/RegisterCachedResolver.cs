using System;

using AlastairLundy.CliInvoke.Core;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace CliInvoke.Extensions.Caching.DependencyInjection;

public static class RegisterCachedResolver
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="serviceLifetime"></param>
    /// <param name="pathCacheLifespan">The timespan to keep the PATH Environment cache available for, defaults to 3 minutes if not set.</param>
    /// <param name="pathExtensionsLifespan">The timespan to keep the PATH Environment Extensions cache available for, defaults to 1 hour if not set.</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public static IServiceCollection UseCachedFilePathResolver(this IServiceCollection services, ServiceLifetime serviceLifetime = ServiceLifetime.Scoped,
        TimeSpan? pathCacheLifespan = null, TimeSpan? pathExtensionsLifespan = null)
    {
        services.AddMemoryCache();
        
        switch (serviceLifetime)
        {
            
            case ServiceLifetime.Scoped:
                services.AddScoped<IFilePathResolver>(sp => 
                        new CachedFilePathResolver(sp.GetRequiredService<IMemoryCache>(),
                            pathCacheLifespan,
                            pathExtensionsLifespan));
                break;
            case ServiceLifetime.Singleton:
                services.AddSingleton<IFilePathResolver>(sp => 
                    new CachedFilePathResolver(sp.GetRequiredService<IMemoryCache>(),
                        pathCacheLifespan,
                        pathExtensionsLifespan));
                break;
            case ServiceLifetime.Transient:
                services.AddTransient<IFilePathResolver>(sp => 
                    new CachedFilePathResolver(sp.GetRequiredService<IMemoryCache>(),
                        pathCacheLifespan,
                        pathExtensionsLifespan));
                break;
            default:
                throw new NotSupportedException();
        }
        
        return services;
    }
}