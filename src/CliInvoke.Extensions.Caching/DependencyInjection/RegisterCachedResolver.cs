/*
    CliInvoke
    Copyright (C) 2024-2025  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
   */

namespace CliInvoke.Extensions.Caching.DependencyInjection;

/// <summary>
/// Provides an extension method to register a cached file path resolver service in the dependency injection container.
/// </summary>
public static class RegisterCachedResolver
{
    /// <param name="services">The <see cref="IServiceCollection"/> to which the cached file path resolver is added.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Configures the application to use a cached file path resolver service with a specified lifetime and optional cache lifespans.
        /// </summary>
        /// <param name="serviceLifetime">The <see cref="ServiceLifetime"/> of the cached file path resolver. Defaults to the value of Scoped.</param>
        /// <param name="filePathCacheLifespan"></param>
        /// <param name="pathCacheLifespan">An optional <see cref="TimeSpan"/> representing the lifespan of the path cache. If null defaults to 3 minutes if not set.</param>
        /// <param name="pathExtensionsLifespan">An optional <see cref="TimeSpan"/> representing the lifespan of the path extension cache. If null, defaults to 1 hour if not set.</param>
        /// <returns>The <see cref="IServiceCollection"/> to allow further configuration chaining.</returns>
        /// <exception cref="NotSupportedException">Thrown when the specified <paramref name="serviceLifetime"/> is not supported.</exception>
        public IServiceCollection UseCachedFilePathResolver(ServiceLifetime serviceLifetime = ServiceLifetime.Scoped,
            TimeSpan? filePathCacheLifespan = null, TimeSpan? pathCacheLifespan = null, TimeSpan? pathExtensionsLifespan = null)
        {
            services.AddMemoryCache();

            switch (serviceLifetime)
            {
                case ServiceLifetime.Scoped:
                    services.TryAddScoped<IFilePathResolver>(sp => 
                        new CachedFilePathResolver(sp.GetRequiredService<IMemoryCache>(),
                            filePathCacheLifespan, pathCacheLifespan, pathExtensionsLifespan));
                    break;
                case ServiceLifetime.Singleton:
                    services.TryAddSingleton<IFilePathResolver>(sp => 
                        new CachedFilePathResolver(sp.GetRequiredService<IMemoryCache>(),
                            filePathCacheLifespan, pathCacheLifespan, pathExtensionsLifespan));
                    break;
                case ServiceLifetime.Transient:
                    services.TryAddTransient<IFilePathResolver>(sp => 
                        new CachedFilePathResolver(sp.GetRequiredService<IMemoryCache>(),
                            filePathCacheLifespan, pathCacheLifespan, pathExtensionsLifespan));
                    break;
                default:
                    throw new NotSupportedException();
            }
        
            return services;
        }
    }
}