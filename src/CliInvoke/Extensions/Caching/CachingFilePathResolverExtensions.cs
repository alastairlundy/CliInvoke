/*
    CliInvoke.Extensions
    Copyright (C) 2024-2026  Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using System.Linq;

using Microsoft.Extensions.Caching.Memory;

namespace CliInvoke.Extensions.Caching;

/// <summary>
///     Provides extension methods that wrap the registered <see cref="IFilePathResolver"/> with a
///     caching decorator backed by <see cref="IMemoryCache"/>.
/// </summary>
public static class CachingFilePathResolverExtensions
{
    /// <param name="services">The service collection to configure.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     Decorates the currently-registered <see cref="IFilePathResolver"/> with
        ///     <see cref="CachingFilePathResolver"/>, using the default caching options.
        /// </summary>
        /// <remarks>
        ///     Registers <see cref="IMemoryCache"/> as a <see cref="ServiceLifetime.Singleton"/> (with the
        ///     default <c>SizeLimit</c> of 512) and re-registers <see cref="IFilePathResolver"/> as the
        ///     caching decorator at the lifetime of the resolver it replaces (default Scoped per Design
        ///     Decision 5). The cache store is shared across scopes; only the wrapper instance is per-scope.
        /// </remarks>
        /// <returns>The service collection for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="services"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when no <see cref="IFilePathResolver"/> is registered (call <c>AddCliInvoke</c> or
        ///     <c>UseCustomFilePathResolver</c> first).
        /// </exception>
        public IServiceCollection UseCachingFilePathResolver()
        {
            ArgumentNullException.ThrowIfNull(services);

            return services.UseCachingFilePathResolver(_ => { });
        }

        /// <summary>
        ///     Decorates the currently-registered <see cref="IFilePathResolver"/> with
        ///     <see cref="CachingFilePathResolver"/>, overriding the caching options.
        /// </summary>
        /// <remarks>
        ///     The inner resolver is captured from the active (last) <see cref="IFilePathResolver"/> registration and
        ///     reconstructed directly (not resolved via <see cref="IFilePathResolver"/> itself) so the decorator
        ///     swap avoids a circular dependency. Because the DI container returns the last registered
        ///     implementation when a single service is resolved, the decorator wraps that active resolver rather
        ///     than an earlier one. <see cref="IMemoryCache"/> is registered as a
        ///     <see cref="ServiceLifetime.Singleton"/> and shared across all scopes.
        /// </remarks>
        /// <param name="configure">A callback to customise <see cref="CachingFilePathResolverOptions"/>.</param>
        /// <returns>The service collection for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="services"/> or <paramref name="configure"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when no <see cref="IFilePathResolver"/> is registered (call <c>AddCliInvoke</c> or
        ///     <c>UseCustomFilePathResolver</c> first).
        /// </exception>
        public IServiceCollection UseCachingFilePathResolver(Action<CachingFilePathResolverOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);

            CachingFilePathResolverOptions options = new CachingFilePathResolverOptions();
            configure(options);

            // Ensure a shared Singleton cache with the configured SizeLimit. TryAdd so a
            // consumer-supplied IMemoryCache is not clobbered.
            services.TryAddSingleton<IMemoryCache>(_ =>
                new MemoryCache(new MemoryCacheOptions { SizeLimit = options.SizeLimit }));

            // The DI container resolves the last registered implementation for a single service
            // (last-wins), so the active resolver is the last one. Capture it so the decorator wraps
            // the resolver that would otherwise be returned, not an earlier registration.
            ServiceDescriptor? innerDescriptor =
                services.LastOrDefault(d => d.ServiceType == typeof(IFilePathResolver));

            if (innerDescriptor is null)
                throw new InvalidOperationException(
                    "No IFilePathResolver is registered. Call AddCliInvoke() or UseCustomFilePathResolver<T>() " +
                    "before UseCachingFilePathResolver().");

            // Capture the existing registration and remove it so the decorator can take its place without
            // creating a circular IFilePathResolver dependency.
            services.RemoveAll<IFilePathResolver>();

            Func<IServiceProvider, IFilePathResolver> innerFactory = sp =>
            {
                if (innerDescriptor.ImplementationFactory is not null)
                    return (IFilePathResolver)innerDescriptor.ImplementationFactory(sp)!;

                if (innerDescriptor.ImplementationInstance is not null)
                    return (IFilePathResolver)innerDescriptor.ImplementationInstance;

                return (IFilePathResolver)ActivatorUtilities.CreateInstance(sp, innerDescriptor.ImplementationType!);
            };

            ServiceLifetime lifetime = innerDescriptor.Lifetime;

            services.Add(ServiceDescriptor.Describe(
                typeof(IFilePathResolver),
                sp => new CachingFilePathResolver(
                    innerFactory(sp),
                    sp.GetRequiredService<IMemoryCache>(),
                    options),
                lifetime));

            return services;
        }
    }
}
